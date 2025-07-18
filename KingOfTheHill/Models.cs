namespace KingOfTheHill;


public class Player
{
    public Guid Id = new();
    public List<ICard> Deck = [];
    public int Score = 0;
    public bool HasCombo = false;
    public List<ICard> LastPlayedCards { get; set; } = new List<ICard>();
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
        double chance = random.NextDouble(); // 0.0 - 1.0
        
        if (chance < 0.8) // 80% обычные карты
        {
            bool isPositive = random.Next(2) == 0; // 50/50 позитивная/негативная
            int value = random.Next(1, 11); // Значение 1-10
            
            return isPositive 
                ? new PositiveCard(value) 
                : new NegativeCard(value);
        }
        else if (chance < 0.95) // 15% специальные карты (0.8-0.95)
        {
            var command = random.Next(2) == 0 
                ? SpecialCommand.Silence 
                : SpecialCommand.ChangeMove;
                
            return new SpecialCard(command);
        }
        else // 5% бонусные карты (0.95-1.0)
        {
            return new BonusCard();
        }
    }
}

public class Game
{
    public int MaxScore = 300;
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