using KingOfTheHill.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using static KingOfTheHill.IGameProvider;

namespace KingOfTheHill.Hubs
{
    public class MainHub : Hub
    {

        private static readonly ConcurrentDictionary<Guid, Game> _games = new();
        private static readonly ConcurrentDictionary<Guid, int> _gamesPlayersCount = new();
        private readonly ILogger _logger;
        private readonly IHubContext<MainHub> _hubContext;
        private readonly IGameProvider _gameProvider;

        public MainHub(ILogger logger, IGameProvider gameProvider, IHubContext<MainHub> hubContext)
        {
            _logger = logger;
            _gameProvider = gameProvider;
            _hubContext = hubContext;
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"The user {Context.ConnectionId} was disconnected");

            var connectionId = Context.ConnectionId;
            var gameId = _games.FirstOrDefault(pair => pair.Value.Players.Any(p => p.ConnectionId == Context.ConnectionId)).Value.GameID;

            await LeaveGameAsync(gameId);

            await base.OnDisconnectedAsync(exception);

        }
    
        public async Task GetActiveGames()
        {
            _logger.LogInformation($"Sending all active games to {Context.ConnectionId}");

            await Clients.All.SendAsync("RefreshGamesList", _games.ToDictionary());
        }

        public async Task CreateGameAsync(string playerName) // Создается лобби
        {
            try
            {
                var player = new Player()
                {
                    ConnectionId = Context.ConnectionId,
                    Name = playerName

                };

                player.Deck.Add(new PositiveCard(5));
                var game = new Game()
                {
                    CurrentPlayer = player.Id,
                    time = DateTime.Now,
                    Players = [player]
                };

                _logger.LogInformation($"Game {game.GameID} was created by {Context.ConnectionId}");

                player.GameId = game.GameID;

                _games.TryAdd(game.GameID, game);
                _gamesPlayersCount.TryAdd(game.GameID, 0);
                _gamesPlayersCount[game.GameID] += 1;

                await Groups.AddToGroupAsync(Context.ConnectionId, game.GameID.ToString());
                await Clients.Caller.SendAsync("JoinGameLobby", game);
                await Clients.All.SendAsync("GameCreated", game); // у создавшего отрисовывается окошка запуска,
                                                                  // все видять лобби
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating game by {Context.ConnectionId}");

                await Clients.Caller.SendAsync("GameCreateError");
                throw;
            }

        }

        public async Task JoinGameAsync(string playerName, Guid gameId) // заходим в лобби
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
                        _gamesPlayersCount[gameId] += 1;
                    }

                    _logger.LogInformation($"User {Context.ConnectionId} was joined game {gameId}");

                    await Groups.AddToGroupAsync(Context.ConnectionId, gameId.ToString());

                    await Clients.Group(gameId.ToString())
                        .SendAsync("JoinGameLobby", _games[gameId]); // у зашедшего отрисовывается окошко лобби

                    await StartGameAsync(gameId);
                }
                else
                {
                    _logger.LogInformation($"The game {gameId} was not found");

                    await Clients.Caller.SendAsync("GameNotFoundOrFull"); // окошко игра не найдена
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error joinin game {gameId} by {Context.ConnectionId}");
            }

        }

        //после этих методов у пользователя вместо лобби должно отрисоваться окошко запуска игры с игроками,
        //с переадрисацией ебля дипсик сосет

        // вот тут должна отрисоваться сама игра
        public async Task StartGameAsync(Guid gameId)
        {

            var currentGamePlayers = _gamesPlayersCount[gameId];

            if (currentGamePlayers == 2)
            {
                var timer = new GameTimerService(_logger);

                timer.game = _games[gameId];
                timer.OnTimerStarted = (g) => Clients.Group(g.ToString()).SendAsync("TimerWasStarted");
                timer.OnTimerUpdate = (g, seconds) => _hubContext.Clients.Group(g.ToString()).SendAsync("UpdateTimer", seconds);
                timer.OnTimerCompleted = (g) => _hubContext.Clients.Group(g.ToString()).SendAsync("StartGame");
                timer.OnTimerStopped = (g) => Clients.Group(g.ToString()).SendAsync("TimerWasStopped");
                timer.TimerStopCondition = (g) => g?.Players?.Count < 2;
                timer.TimerCompletedCondition = (g) => g?.Players?.Count < 4;
                await timer.StartTimer(60);
            }
        }

        public async Task RestartGameAsync(Guid gameId)
        {
            _logger.LogInformation($"Restarting game {gameId}");

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

            await Clients.Group(gameId.ToString()).SendAsync("GameRestarted"); // придется выкинуть из комнаты с
                                                                               // игрой и вернуться к окошку запуска игры
                                                                               // иначе ебля
        }

        public async Task LeaveGameAsync(Guid gameId)
        {
            try
            {
                _logger.LogInformation($"The user {Context.ConnectionId} is leaving game {gameId}");

                if (!_games.TryGetValue(gameId, out var game)) 
                {
                    _logger.LogInformation($"While leaving game, the game {gameId} was not found");

                    await Clients.Caller.SendAsync("GameNotFound");
                    return;
                }

                lock (game.Players)
                {
                    var player = game.Players.First(p => p.ConnectionId == Context.ConnectionId);

                    if (player is null)
                    {
                        _logger.LogInformation($"While leaving game {gameId} the user {Context.ConnectionId} was not found");

                        Clients.Caller.SendAsync("PlayerNotFound");
                        return;
                    }

                    game.Players.Remove(player);
                    _gamesPlayersCount[gameId] -= 1;
                }

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId.ToString());

                _gamesPlayersCount.TryGetValue(gameId, out var count);

                if (count == 0)
                {
                    _logger.LogInformation($"The game {gameId} is empty, deliting game...");

                    _games.TryRemove(gameId, out _);
                    _gamesPlayersCount.TryRemove(gameId, out _);
                }

                await Clients.Caller.SendAsync("ToMainPage"); // возвращаем к списку лобби
            }
            catch (Exception ex)
            {
                _logger.LogError($"While user {Context.ConnectionId} was leaving the game {gameId} occured error", ex);
            }
        }

        public (Game? game, Player? player) GetCurrentGameAndPlayer()
        {
            var game = _games.FirstOrDefault(pair =>
                pair.Value.Players.Any(p => p.ConnectionId == Context.ConnectionId)).Value;

            if (game == null) return (null, null);

            var player = game.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
            return (game, player);
        }

        // дальше во всех SendAsync просто отрисовываем изменения в игре

        public async Task DrawCard()
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

        public async Task PassTheMove()
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

        public async Task UseCardAttachedToPlayer(ICard card, Player? targetPlayer = null)
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

        public async Task UseCardAttachedToGame(ICard card)
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

        public async Task EndMove()
        {
            await PassTheMove();
        }
    }
}
