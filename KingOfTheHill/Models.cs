namespace KingOfTheHill;


public class Player
{
    public Guid Id = new();
    public List<ICard> Deck = [];
    public int Score = 0;
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

public class SpecialCard(SpecialCommand command)
{
    public SpecialCommand Value = command;

    public void Invoke(Player player)
    {
        throw new NotImplementedException();
    }
}

public class Game
{
    public int MaxScore = 300;
    public Direction direction = Direction.Left;
    public Guid CurrentPlayer;
    public DateTime time;
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