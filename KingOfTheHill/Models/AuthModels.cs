namespace KingOfTheHill.Models;

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string AccesToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
}

public class RefreshTokenResponse
{
    public string AccesToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
}

public class RefreshTokenRequest
{ 
    public string RefreshToken { get; set; } = null!;
}
