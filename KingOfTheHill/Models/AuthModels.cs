namespace KingOfTheHill.Models;

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
}
