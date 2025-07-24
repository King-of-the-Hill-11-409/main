using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace KingOfTheHill;

public interface IGameProvider
{
    void DrawCard(ref Player player);
    void PassTheMove(ref Game game);
    void UseCardAttachedToPlayer(ICard card, ref Player player);
    void UseCardAttachedToGame(ICard card, ref Game game);
}
public class GameProvider : IGameProvider // ToDo
{
    public void DrawCard(ref Player player)
    {
        for (int i = player.Deck.Count; i < 6; i++)
            player.Deck.Add(CardDeck.DrawRandomCard());

        player = new Player()
        {
            ConnectionId = player.ConnectionId,
            Id = player.Id,
            GameId = player.GameId,
            isFreezed = player.isFreezed,
            isSkipTurn = player.isSkipTurn,
            Name = player.Name,
            Deck = player.Deck,
            Score = player.Score,
            HasCombo = player.HasCombo,
        };
    }

    public void PassTheMove(ref Game game)
    {
        var localGame = game;

        int currentIndex = game.Players.FindIndex(player => player.Id == localGame.CurrentPlayer);

        int nextIndex = game.direction switch
        {
            Direction.Right => (currentIndex + 1) % game.Players.Count,
            Direction.Left => (currentIndex - 1 + game.Players.Count) % game.Players.Count,
            _ => throw new NotImplementedException(),
        };

        game.CurrentPlayer = game.Players[nextIndex].Id;

        game = new Game()
        {
            GameID = game.GameID,
            direction = game.direction,
            CurrentPlayer = game.CurrentPlayer,
            time = game.time,
            Players = game.Players,
            isStarted = game.isStarted,
        };
    }

    public void UseCardAttachedToGame(ICard card, ref Game game)
    {
        if (card is SpecialCard special)
        {
            var localGame = game;

            switch (special.Value)
            {
                case SpecialCommand.Silence:
                    int currentIndex = game.Players.FindIndex(player => player.Id == localGame.CurrentPlayer);
                    int nextIndex = game.direction switch
                    {
                        Direction.Right => (currentIndex + 1) % game.Players.Count,
                        Direction.Left => (currentIndex - 1 + game.Players.Count) % game.Players.Count,
                        _ => throw new NotImplementedException(),
                    };
                    game.Players[nextIndex].isSkipTurn = true;
                    break;
                case SpecialCommand.ChangeMove:
                    game.direction = game.direction == Direction.Right ? Direction.Left : Direction.Right;
                    break;
            }
        }
    }

    public void UseCardAttachedToPlayer(ICard card, ref Player p)
    {
        switch (card)
        {
            case PositiveCard positiveCard:
                p.Score = positiveCard.Invoke(p);
                break;
            case NegativeCard negativeCard:
                int Score = negativeCard.Invoke(p);
                p.Score = Score < 0 ? p.Score : Score;
                break;
            case BonusCard bonusCard:
                p.Score = bonusCard.Invoke(p);
                break;
        }

        p = new()
        {
            ConnectionId = p.ConnectionId,
            Id = p.Id,
            GameId = p.GameId,
            isFreezed = p.isFreezed,
            isSkipTurn = p.isSkipTurn,
            Name = p.Name,
            Deck = p.Deck,
            Score = p.Score,
            HasCombo = p.HasCombo,
        };
    }
}

public class Player
{

    public string ConnectionId { get; set; } = null!;

    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid GameId { get; set; }

    public bool isFreezed { get; set; } = false;

    public bool isSkipTurn { get; set; } = false;

    public string Name { get; set; } = null!;

    public List<ICard> Deck { get; set; } = [];

    public List<ICard> LastPlayedCards { get; set; } = new List<ICard>();

    public int Score { get; set; } = 10;

    public bool HasCombo { get; set; } = false;
}

public class ICard
{
    public Guid Id = Guid.NewGuid();

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        var other = (ICard)obj;
        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}


public class PositiveCard(int value) : ICard
{
    public int Value = value;
    public int Invoke(Player player) => player.Score + Value;
}

public class NegativeCard(int value) : ICard
{
    public int Value = value;
    public int Invoke(Player player) => player.Score - Value;
}

public class BonusCard : ICard
{
    public int Invoke(Player player) => player.Score * 2;
}

public class SpecialCard(SpecialCommand command) : ICard
{
    public SpecialCommand Value = command;

    public void Invoke(Player player)
    {
        throw new NotImplementedException();
    }
}

public static class CardDeck
{
    private static readonly Random random = new();

    public static ICard DrawRandomCard()
    {
        double chance = random.NextDouble();

        if (chance < 0.8)
        {
            bool isPositive = random.Next(2) == 0;
            int value = random.Next(1, 11);

            return isPositive
                ? new PositiveCard(value)
                : new NegativeCard(value);
        } 
        else if (chance < 0.95)
        {
            var command = random.Next(2) == 0
                ? SpecialCommand.Silence
                : SpecialCommand.ChangeMove;

            return new SpecialCard(command);
        }
        else
        {
            return new BonusCard();
        }
    }
}

public class Game
{
    public int MaxScore { get; set; } = 300;

    public Guid GameID { get; set; } = Guid.NewGuid();

    public bool isStarted { get; set; } = false;

    public Direction direction { get; set; } = Direction.Right;

    public Guid CurrentPlayer { get; set; }

    public DateTime time { get; set; }

    public List<Player> Players { get; set; } = [];
}

public enum Direction
{
    Right,
    Left
}

public enum SpecialCommand
{
    Silence,
    ChangeMove
}