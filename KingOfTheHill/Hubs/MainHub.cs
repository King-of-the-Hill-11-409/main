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
            try
            {
                var gameId = _games.FirstOrDefault(pair => pair.Value.Players.Any(p => p.ConnectionId == Context.ConnectionId)).Value.GameID;
                await LeaveGameAsync(gameId);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Blazor Bug {ex}");
            }

            await base.OnDisconnectedAsync(exception);

        }

        /// <summary>
        /// Gets all active games and sends to client`s method RefreshGamesList
        /// </summary>
        /// <returns>Task</returns>
        public async Task GetActiveGames()
        {
            _logger.LogInformation($"Sending all active games to {Context.ConnectionId}");

            await Clients.Caller.SendAsync("RefreshGamesList", _games.ToDictionary());
        }

        /// <summary>
        /// Creating a new Game and adds it to active games, sends to client`s methods JoinGAmeLobby and GameCreated created game
        /// </summary>
        /// <param name="playerName">Player name who calls this method</param>
        /// <returns>Task</returns>
        public async Task CreateGameAsync(Guid playerId, string username) // Создается лобби
        {
            try
            {
                Player player = new()
                {
                    ConnectionId = Context.ConnectionId,
                    Name = username,
                    Id = playerId,
                };

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

        /// <summary>
        /// Adds user to active game with gameId ID, calls the JoinGameLobby and StartGameAsync"
        /// </summary>
        /// <param name="playerName">Player name who calls the method</param>
        /// <param name="gameId">Game ID ih which player consist</param>
        /// <returns>Task</returns>
        public async Task JoinGameAsync(Guid playerId, string username, Guid gameId)
        {
            try
            {
                if (_games.ContainsKey(gameId) && _gamesPlayersCount[gameId] < 4)
                {
                    Player player = new()
                    {
                        ConnectionId = Context.ConnectionId,
                        Name = username,
                        Id = playerId,
                        GameId = gameId,
                    };

                    if (_games[gameId].isStarted) player.isFreezed = true;

                    lock (_games[gameId].Players)
                    {
                        _games[gameId].Players.Add(player);
                        _gamesPlayersCount[gameId] += 1;
                    }

                    _logger.LogInformation($"User {Context.ConnectionId} was joined game {gameId}");

                    await Groups.AddToGroupAsync(Context.ConnectionId, gameId.ToString());

                    await Clients.All.SendAsync("RenderAfterJoinUser", gameId, _games[gameId]);

                    await Clients.Group(gameId.ToString())
                        .SendAsync("JoinGameLobby", _games[gameId]);

                    await StartGameAsync(gameId);
                }
                else
                {
                    _logger.LogInformation($"The game {gameId} was not found");

                    await Clients.Caller.SendAsync("GameNotFoundOrFull");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error joinin game {gameId} by {Context.ConnectionId}");
            }

        }

        /// <summary>
        /// Starts the timer which upon completition calls the StartGame method on client
        /// </summary>
        /// <param name="gameId">Game Id in which the caller consist</param>
        /// <returns>Task</returns>
        public async Task StartGameAsync(Guid gameId)
        {

            var currentGamePlayers = _gamesPlayersCount[gameId];

            if (currentGamePlayers == 2)
            {
                var timer = new GameTimerService(_logger);

                timer.game = _games[gameId];
                timer.OnTimerStarted = (g) => _hubContext.Clients.Group(g.ToString()).SendAsync("TimerWasStarted");
                timer.OnTimerUpdate = (g, seconds) => _hubContext.Clients.Group(g.ToString()).SendAsync("UpdateTimer", seconds);
                timer.OnTimerCompleted = (g) =>
                {
                    for (int i = 0; i < _games[gameId].Players.Count; i++)
                    {
                        Player player = _games[gameId].Players[i];
                        _gameProvider.DrawCard(ref player);
                        _games[gameId].Players[i] = player;
                    }
                    _games[gameId].isStarted = true;
                    return _hubContext.Clients.Group(g.ToString()).SendAsync("StartGame", _games[gameId]);
                };
                timer.OnTimerStopped = (g) => _hubContext.Clients.Group(g.ToString()).SendAsync("TimerWasStopped");
                timer.TimerStopCondition = (g) => g?.Players?.Count < 2;
                timer.TimerCompletedCondition = (g) => g?.Players?.Count < 4;
                await timer.StartTimer(60);
            }
        }

        /// <summary>
        /// Restarts the game with gameId and calls the GameRestarted method on client
        /// </summary>
        /// <param name="gameId">Game id in which the caller consist</param>
        /// <returns>Task</returns>
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

            await Clients.Group(gameId.ToString()).SendAsync("GameRestarted");
        }

        /// <summary>
        /// Delete the callor from game, and delits the game if there is no one in it, calls the ToMainPage mrthod on client
        /// </summary>
        /// <param name="gameId">The id of game where is the caller consist</param>
        /// <returns>Task</returns>
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

                var player = game.Players.First(p => p.ConnectionId == Context.ConnectionId);
                lock (game)
                {
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

                    await Clients.All.SendAsync("DeleteGameFromLobby", gameId);
                }
                else if (count > 0)
                {
                    if (game.CurrentPlayer == player.Id)
                        game.CurrentPlayer = game.Players[0].Id;
                    await Clients.Group(gameId.ToString()).SendAsync("UpdateAfterLeave", game);
                }

                await Clients.Caller.SendAsync("ToMainPage");
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

        /// <summary>
        /// Gives a random card to player, calls the ChangePlayerState method on client
        /// </summary>
        /// <returns>Task</returns>
        public async Task DrawCard()
        {
            var (game, player) = GetCurrentGameAndPlayer();

            _logger.LogInformation($"The player {Context.ConnectionId} gets the card");

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

        /// <summary>
        /// Pass the move to next player and calls the ChangeGameState method on client
        /// </summary>
        /// <returns>Task</returns>
        public async Task PassTheMove()
        {
            var (game, player) = GetCurrentGameAndPlayer();
            if (game == null || player == null) return;

            _logger.LogInformation($"The player {Context.ConnectionId} is passing the move");

            try
            {
                player.isSkipTurn = false;
                player.HasCombo = false;

                _gameProvider.PassTheMove(ref game);
                await Clients.Group(game.GameID.ToString()).SendAsync("ChangeGameState", game);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PassTheMoveError");

                await Clients.Caller.SendAsync("Error", "Failed to pass the move");
            }
        }

        /// <summary>
        /// Use card which change the player state(player score) and calls ChangePlayerState method on client
        /// </summary>
        /// <param name="card">Card</param>
        /// <param name="targetPlayer">Target player</param>
        /// <returns></returns>
        public async Task UseCardAttachedToPlayer(int cardIndex, Player? targetPlayer = null)
        {
            var (game, player) = GetCurrentGameAndPlayer();
            targetPlayer ??= player;

            if (game == null || targetPlayer == null || player == null) return;

            _logger.LogInformation($"The player {Context.ConnectionId} is using the card attached to player {targetPlayer.ConnectionId}");

            try
            {
                ICard card = player.Deck[cardIndex];
                player.Deck = [.. player.Deck.Where(c => c != card)];
                player.HasCombo = false;

                switch (card)
                {
                    case PositiveCard positiveCard:
                        player.Score = positiveCard.Invoke(player);
                        break;
                    case NegativeCard negativeCard:
                        int Score = negativeCard.Invoke(targetPlayer);
                        targetPlayer.Score = Score < 0 ? targetPlayer.Score : Score;
                        break;
                    case BonusCard bonusCard:
                        player.Score = bonusCard.Invoke(player);
                        break;
                }

                if (targetPlayer.Id != player.Id)
                    game.Players[game.Players.FindIndex(p => p.Id == targetPlayer.Id)] = targetPlayer;
                
                await DrawCard();
                await EndMove();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UseCardAttachedToPlayerError");
                await Clients.Caller.SendAsync("Error", "Failed to use card");
            }
        }

        public async Task UseComboAttachedToPlayer(List<int> cardsIndex, Player? targetPlayer = null)
        {
            var (game, player) = GetCurrentGameAndPlayer();
            targetPlayer ??= player;

            if (game == null || targetPlayer == null || player == null) return;

            _logger.LogInformation($"The player {Context.ConnectionId} is using the card attached to player {targetPlayer.ConnectionId}");

            try
            {
                List<ICard> cards = [.. cardsIndex.Select(c => player.Deck[c])];
                player.Deck = [.. player.Deck.Where((_, i) => !cardsIndex.Contains(i))];
                player.HasCombo = true;

                foreach (var card in cards)
                {
                    switch (card)
                    {
                        case PositiveCard positiveCard:
                            player.Score = positiveCard.Invoke(player);
                            break;
                        case NegativeCard negativeCard:
                            int Score = negativeCard.Invoke(targetPlayer);
                            targetPlayer.Score = Score < 0 ? targetPlayer.Score : Score;
                            break;
                    }
                }

                if (targetPlayer.Id != player.Id)
                    game.Players[game.Players.FindIndex(p => p.Id == targetPlayer.Id)] = targetPlayer;

                await DrawCard();
                await Clients.Group(game.GameID.ToString()).SendAsync("ChangeGameState", game);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UseComboAttachedToPlayerError");
                await Clients.Caller.SendAsync("Error", "Failed to use combo");
            }
        }

        /// <summary>
        /// Use card attached to game which change the game state and calls the ChangeGameState method on client
        /// </summary>
        /// <param name="card">Card</param>
        /// <returns>Task</returns>
        public async Task UseCardAttachedToGame(int cardIndex)
        {
            var (game, player) = GetCurrentGameAndPlayer();
            if (game == null || player == null) return;

            _logger.LogInformation($"The player {Context.ConnectionId} is using card attached to game");
            try
            {
                ICard card = player.Deck[cardIndex];
                player.Deck = [.. player.Deck.Where(c => c != card)];
                _gameProvider.UseCardAttachedToGame(card, ref game);

                await DrawCard();
                await EndMove();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UseCardAttachedToGameError");
                await Clients.Caller.SendAsync("Error", "Failed to use game card");
            }
        }

        /// <summary>
        /// Ends the move of player
        /// </summary>
        /// <returns>Task</returns>
        public async Task EndMove()
        {
            _logger.LogInformation($"Player {Context.ConnectionId} has ended his turn");

            await PassTheMove();
        }
    }
}
