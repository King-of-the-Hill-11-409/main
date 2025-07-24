using Bunit;
using KingOfTheHill;
using KingOfTheHill.Components.Pages;

namespace TestsBlazorComponents
{
    public class PlayComponentTest : TestContext
    {
        [Fact]
        public void RendersLobbyByDefault()
        {
            var component = RenderComponent<Play>();

            Assert.NotNull(component.Find(".lobby__container"));
            Assert.NotNull(component.Find("input[placeholder='Введите Ваш никнейм']"));
        }

        [Fact]
        public void UsernameInput_DisabledWhenGameExists()
        {
            var component = RenderComponent<Play>();

            component.Instance._currentGame = new Game();
            component.Render();

            var input = component.Find("input");
            Assert.True(input.HasAttribute("disabled"));
        }

        [Fact]
        public void CreateLobbyButton_DisabledWithoutUsername()
        {
            var component = RenderComponent<Play>();

            var button = component.Find("button.list__createButton");
            Assert.True(button.HasAttribute("disabled"));
        }

        [Fact]
        public void LobbyList_RendersCorrectNumberOfGames()
        {
            var component = RenderComponent<Play>();
            component.Instance.Lobbies = new Dictionary<Guid, Game>
            {
                [Guid.NewGuid()] = new Game { Players = new List<Player>(1) },
                [Guid.NewGuid()] = new Game { Players = new List<Player>(2) }
            };

            component.Render();

            var items = component.FindAll(".list__lobby div");

            Assert.Equal(2, items.Count);
        }

        [Fact]
        public void GameView_RendersWhenGameStarted()
        {
            var component = RenderComponent<Play>();
            var playerId = Guid.NewGuid();

            component.Instance._currentGame = new Game
            {
                isStarted = true,
                Players = new List<Player>
                {
                    new Player
                    {
                        Id = playerId,
                        Name = "TestPlayer",
                        Deck = new List<ICard>(),
                        ConnectionId = "test-connection"
                    }
                },
                CurrentPlayer = playerId
            };
            component.Instance._currentPlayerId = playerId;
            component.Instance._currentPlayer = component.Instance._currentGame.Players[0];

            component.Render();

            Assert.NotNull(component.Find(".game__container"));
        }

        [Fact]
        public void PlayerHand_RendersCorrectNumberOfCards()
        {
            var component = RenderComponent<Play>();
            var playerId = Guid.NewGuid();
            component.Instance._currentGame = new Game
            {
                isStarted = true,
                Players = new List<Player>
                {
                    new Player
                    {
                        Id = playerId,
                        Name = "TestPlayer",
                        Deck = new List<ICard>
                        {
                            new PositiveCard(1),
                            new NegativeCard(2)
                        },
                        ConnectionId = "test-connection"

                    }
                },
                CurrentPlayer = playerId

            };

            component.Instance._currentPlayerId = playerId;
            component.Instance._currentPlayer = component.Instance._currentGame.Players[0];
            component.Render();

            var cards = component.FindAll(".hand__card");


            Assert.Equal(2, cards.Count);
        }

        [Fact]
        public void CardSelection_TogglesCardInComboHand()
        {
            var component = RenderComponent<Play>();
            var playerId = Guid.NewGuid();

            component.Instance._currentGame = new Game
            {
                isStarted = true,
                Players = new List<Player>
                {
                    new Player
                    {   
                        Id = playerId,
                        Name = "TestPlayer",
                        Deck = new List<ICard> { new PositiveCard(1) }
                    }
                },
                CurrentPlayer = playerId
            };

            component.Instance._currentPlayer = component.Instance._currentGame.Players[0];
            component.Instance._currentPlayerId = component.Instance._currentGame.Players[0].Id;

            component.Render();

            component.Instance.CardHandler(0);

            component.Render();

            Assert.Single(component.Instance._comboHand);
            Assert.Contains(0, component.Instance._comboHand);
        }

        [Fact]
        public void PlayerSelection_UpdatesSelectedPlayer()
        {
            var component = RenderComponent<Play>();
            var testPlayer = new Player { Id = Guid.NewGuid() };
            component.Instance._currentGame = new Game
            {
                isStarted = true,
                CurrentPlayer = Guid.NewGuid(),
                Players = new List<Player> { testPlayer }
            };

            component.Instance._currentPlayer = component.Instance._currentGame.Players[0];
            component.Instance._currentPlayerId = component.Instance._currentGame.CurrentPlayer;

            component.Render();

            component.Instance.SelectPlayer(testPlayer);

            component.Render();

            Assert.Equal(testPlayer, component.Instance._selectedPlayer);
        }

        [Fact]
        public void ShowsWarning_WhenNegativeCardWithoutTarget()
        {
            var component = RenderComponent<Play>();

            var playerId = Guid.NewGuid();
            var testPlayer = new Player
            {
                Id = playerId,
                Name = "TestPlayer",
                Deck = new List<ICard> { new NegativeCard(1) },
                ConnectionId = "test-conn",
                GameId = Guid.NewGuid()
            };

            component.Instance._currentGame = new Game
            {
                isStarted = true,
                CurrentPlayer = playerId,
                Players = new List<Player> { testPlayer }
            };

            component.Instance._currentPlayerId = playerId;
            component.Instance._currentPlayer = testPlayer;
            component.Instance._comboHand = new List<int> { 0 };
            component.Instance._selectedPlayer = null; 

            component.Render();

            component.Instance.DoMovePlayer();

            component.Render();

            Assert.True(component.Instance._warning);
            Assert.Contains("Вы должны указать на кого применить данную карту", component.Markup);
        }

        [Fact]
        public void PlayButton_DisabledWhenNoCardsSelected()
        {
            var component = RenderComponent<Play>();
            var playerId = Guid.NewGuid();

            component.Instance._currentGame = new Game
            {
                isStarted = true,
                CurrentPlayer = playerId,
                Players = new List<Player>
                {
                    new Player
                    {
                        Id = playerId,
                        Name = "TestPlayer",
                        Deck = new List<ICard>(),
                        ConnectionId = "test-conn"
                    }
                }
            };
            component.Instance._currentPlayerId = playerId;
            component.Instance._currentPlayer = component.Instance._currentGame.Players[0];
            component.Instance._comboHand = new List<int>(); 

            component.Render(); 
            var button = component.Find(".playButton__card button");

            Assert.True(button.HasAttribute("disabled"));
        }
    }
}
