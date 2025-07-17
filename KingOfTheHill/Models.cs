namespace KingOfTheHill;

public interface IGameProvider
{
    void DrawCard(Player player);
    void PassTheMove(Game game); // Возвращяет игру с измененным текущим игроком
    void UseCardAttachedToPlayer(ICard card, Player player); // Возвращает измененнного игрока
    void UseCardAttachedToGame(ICard card); // Возвращает измененную игру
}

public class GameProvider : IGameProvider // ToDo
{
    public void DrawCard(Player player)
    {
        throw new NotImplementedException();
    }

    public void PassTheMove(Game game)
    {
        throw new NotImplementedException();
    }

    public void UseCardAttachedToGame(ICard card)
    {
        throw new NotImplementedException();
    }

    public void UseCardAttachedToPlayer(ICard card, Player p)
    {
        throw new NotImplementedException();
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