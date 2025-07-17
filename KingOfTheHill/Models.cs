namespace KingOfTheHill;

public interface IGameProvider
{
    void DrawCard(Player player);
    void PassTheMove(Game game); // ���������� ���� � ���������� ������� �������
    void UseCardAttachedToPlayer(ICard card, Player player); // ���������� ������������ ������
    void UseCardAttachedToGame(ICard card, Game game); // ���������� ���������� ����
}

public class GameProvider : IGameProvider // ToDo
{
    public void DrawCard(Player player)
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

    public void PassTheMove(Game game)
    {
        int currentIndex = game.Players.FindIndex(player => player.Id == game.CurrentPlayer);

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
            Players = game.Players
        };
    }

    public void UseCardAttachedToGame(ICard card, Game game)
    {
        if (card is SpecialCard special)
        {
            switch (special.Value)
            {
                case SpecialCommand.Silence:
                    int currentIndex = game.Players.FindIndex(player => player.Id == game.CurrentPlayer);
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

    public void UseCardAttachedToPlayer(ICard card, Player p)
    {
        switch (card)
        {
            case PositiveCard positiveCard:
                p.Score = positiveCard.Invoke(p);
                break;
            case NegativeCard negativeCard:
                p.Score = negativeCard.Invoke(p);
                break;
            case BonusCard bonusCard:
                p.Score = bonusCard.Invoke(p);
                break;
        }

        p = new Player()
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
    public string ConnectionId = null!;
    public Guid Id = new();
    public Guid GameId;
    public bool isFreezed = false;
    public bool isSkipTurn = false;
    public string Name = null!;
    public List<ICard> Deck = [];
    public List<ICard> LastPlayedCards { get; set; } = new List<ICard>();
    public int Score = 10;
    public bool HasCombo = false;
}

public interface ICard;


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
    public int MaxScore = 300;
    public Guid GameID;
    public bool isStarted = false;
    public Direction direction = Direction.Left;
    public Guid CurrentPlayer;
    public DateTime time;
    public List<Player> Players = [];
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