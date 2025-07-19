using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using static KingOfTheHill.IGameProvider;

namespace KingOfTheHill.Hubs
{
    public class MainHub : Hub
    {

        private static readonly ConcurrentDictionary<Guid, Game> _games = new();
        private static readonly ConcurrentDictionary<Guid, int> _gamesPlayersCount = new();
        private static readonly ConcurrentDictionary<Guid, CancellationTokenSource> _cancellationTokens = new();
        private readonly ILogger _logger;
        private readonly IGameProvider _gameProvider;

        public MainHub(ILogger logger, IGameProvider gameProvider)
        {
            _logger = logger;
            _gameProvider = gameProvider;
        }

        private async Task GetActiveGames()
        {
            await Clients.All.SendAsync("RefreshGamesList", _games);
        }

        private async Task CreateGameAsync(string playerName) // Создается лобби
        {
            try
            {
                var player = new Player()
                {
                    ConnectionId = Context.ConnectionId,
                    Name = playerName

                };
                var game = new Game()
                {
                    GameID = Guid.NewGuid(),
                    CurrentPlayer = player.Id,
                    time = DateTime.Now
                };

                player.GameId = game.GameID;

                _games.TryAdd(game.GameID, game);
                _gamesPlayersCount[game.GameID] += 1;

                await Groups.AddToGroupAsync(Context.ConnectionId, game.GameID.ToString());
                await Clients.Caller.SendAsync("JoinGameLobby", game);
                await Clients.All.SendAsync("GameCreated", game); // у создавшего отрисовывается окошка запуска,
                                                                  // все видять лобби
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("GameCreateError");
                _logger.LogError(ex, "CreateGameAsynError");
                throw;
            }

        }

        private async Task JoinGameAsync(string playerName, Guid gameId) // заходим в лобби
        {
            try
            {
                if (_games.ContainsKey(gameId) && _gamesPlayersCount[gameId] < 4)
                {

                    var player = new Player()
                    {
                        ConnectionId = Context.ConnectionId,
                        Name = playerName,
                        GameId = gameId
                    };

                    if (_games[gameId].isStarted) player.isFreezed = true;

                    lock (_games[gameId].Players)
                    {
                        _games[gameId].Players.Add(player);
                    }

                    _gamesPlayersCount.TryGetValue(gameId, out int val);

                    Interlocked.Increment(ref val);

                    await Groups.AddToGroupAsync(Context.ConnectionId, gameId.ToString());

                    await Clients.Group(gameId.ToString())
                        .SendAsync("JoinGameLobby", _games[gameId]); // у зашедшего отрисовывается окошко лобби

                    var currentCount = _gamesPlayersCount[gameId];

                    if (currentCount == 4)
                    {
                        if (_cancellationTokens.TryGetValue(gameId, out var cts))
                        {
                            cts.Cancel();
                            _cancellationTokens.TryRemove(gameId, out _);
                        }

                        await StartGameAsync(gameId);
                    }

                    else if (currentCount == 2 && !_cancellationTokens.ContainsKey(gameId))
                    {
                        var cts = new CancellationTokenSource();

                        if (!_cancellationTokens.TryAdd(gameId, cts))
                        {
                            cts.Dispose();
                        }

                        await StartGameByTimerAsync(gameId, _cancellationTokens[gameId].Token);
                        await Clients.Group(gameId.ToString()).SendAsync("TimerWasStarted"); // у всех кто в лобби
                                                                                             // (по определению есть окошко запуска)
                                                                                             // видят таймер
                    }
                }
                else
                {
                    await Clients.Caller.SendAsync("GameNotFoundOrFull"); // окошко игра не найдена
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joinin game");
            }

        }

        //после этих методов у пользователя вместо лобби должно отрисоваться окошко запуска игры с игроками,
        //с переадрисацией ебля дипсик сосет

        private async Task StartGameByTimerAsync(Guid gameId, CancellationToken cts) // запускаем таймер если набралось
                                                                                     // два человека и запускаем игру
        {

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), cts); // можно минуту сделать параметром

                if (_gamesPlayersCount[gameId] >= 2)
                {
                    await StartGameAsync(gameId);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation($"The start of {gameId} was canceled");
                await Clients.Group(gameId.ToString()).SendAsync("TimerWasCancelled"); // окошко остается таймер убирается
            }
            finally
            {
                if(_cancellationTokens.TryRemove(gameId, out var ct)) ct.Dispose();
            }
        }

        // вот тут должна отрисоваться сама игра
        private async Task StartGameAsync(Guid gameId)
        {
            await Clients.Group(gameId.ToString()).SendAsync("StartGame", _games[gameId]); //Уведомляем пользователей
                                                                                           //что игра началась (снимаем disabled с интерфейса)
        }

        private async Task RestartGameAsync(Guid gameId)
        {

            lock (_games[gameId])
            {
                var game = new Game()
                {
                    Players = _games[gameId].Players
                    .Select(p => new Player()
                    {
                        ConnectionId = p.ConnectionId,
                        Name = p.Name,
                        GameId = gameId
                    })
                    .ToList(),
                    GameID = gameId,
                    time = DateTime.Now
                };
                game.CurrentPlayer = game.Players[0].Id;
            }
            var cts = new CancellationTokenSource();

            if(!_cancellationTokens.TryAdd(gameId, cts)) cts.Dispose();

            await Clients.Group(gameId.ToString()).SendAsync("GameRestarted"); // придется выкинуть из комнаты с
                                                                               // игрой и вернуться к окошку запуска игры
                                                                               // иначе ебля
            await StartGameByTimerAsync(gameId, cts.Token);
        }

        private async Task LeaveGameAsync(Guid gameId)
        {
            try
            {
                if (!_games.TryGetValue(gameId, out var game)) 
                {
                    await Clients.Caller.SendAsync("GameNotFound");
                    return;
                } 

                lock(game.Players)
                {
                    var player = game.Players.First(p => p.ConnectionId == Context.ConnectionId);

                    if (player is null)
                    {
                        Clients.Caller.SendAsync("PlayerNotFound");
                        return;
                    } 

                    game.Players.Remove(player);
                }

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId.ToString());

                _gamesPlayersCount.TryGetValue(gameId, out var count);
                Interlocked.Decrement(ref count);

                if (count == 0)
                {
                    _games.TryRemove(gameId, out _);
                    _gamesPlayersCount.TryRemove(gameId, out _);
                    _cancellationTokens.TryRemove(gameId, out var cts);
                    cts?.Dispose();
                }

                await Clients.Caller.SendAsync("ToMainPage"); // возвращаем к списку лобби
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving game");
            }
        }

        private (Game? game, Player? player) GetCurrentGameAndPlayer()
        {
            var game = _games.FirstOrDefault(pair =>
                pair.Value.Players.Any(p => p.ConnectionId == Context.ConnectionId)).Value;

            if (game == null) return (null, null);

            var player = game.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
            return (game, player);
        }

        // дальше во всех SendAsync просто отрисовываем изменения в игре

        private async Task DrawCard()
        {
            var (game, player) = GetCurrentGameAndPlayer();

            if (game == null || player == null || player.Deck.Count >= 6)
                return;

            try
            {
                _gameProvider.DrawCard(ref player);
                await Clients.Group(game.GameID.ToString()).SendAsync("ChangePlayerState", player);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DrawCardError");
                await Clients.Caller.SendAsync("Error", "Failed to draw card");
            }
        }

        private async Task PassTheMove()
        {
            var (game, _) = GetCurrentGameAndPlayer();
            if (game == null) return;

            try
            {
                _gameProvider.PassTheMove(ref game);
                await Clients.Group(game.GameID.ToString()).SendAsync("ChangeGameState", game);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PassTheMoveError");
                await Clients.Caller.SendAsync("Error", "Failed to pass the move");
            }
        }

        private async Task UseCardAttachedToPlayer(ICard card, Player? targetPlayer = null)
        {
            var (game, player) = GetCurrentGameAndPlayer();
            targetPlayer ??= player;

            if (game == null || targetPlayer == null) return;

            try
            {
                _gameProvider.UseCardAttachedToPlayer(card, ref targetPlayer);
                await Clients.Group(game.GameID.ToString()).SendAsync("ChangePlayerState", targetPlayer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UseCardAttachedToPlayerError");
                await Clients.Caller.SendAsync("Error", "Failed to use card");
            }
        }

        private async Task UseCardAttachedToGame(ICard card)
        {
            var (game, _) = GetCurrentGameAndPlayer();
            if (game == null) return;

            try
            {
                _gameProvider.UseCardAttachedToGame(card, ref game);
                await Clients.Group(game.GameID.ToString()).SendAsync("ChangeGameState", game);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UseCardAttachedToGameError");
                await Clients.Caller.SendAsync("Error", "Failed to use game card");
            }
        }

        private async Task EndMove()
        {
            await PassTheMove();
        }
    }
}
