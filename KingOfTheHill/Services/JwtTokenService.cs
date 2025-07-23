using KingOfTheHill.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace KingOfTheHill.Services;

public class JwtTokenFactory
{
    private readonly IConfiguration _config;
    private readonly ILogger _logger;
    private JwtSecurityTokenHandler JWTHandler= new JwtSecurityTokenHandler();

    public JwtTokenFactory(IConfiguration config, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    public string CreateToken(User user, DateTime time)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Name),
            new("UserId", user.Id.ToString()),
            new(ClaimTypes.Role, "Player")
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: time,
            signingCredentials: new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string RefreshToken(string expiredToken, DateTime time)
    {
        var handler = new JwtSecurityTokenHandler();
        var oldToken = handler.ReadJwtToken(expiredToken);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var newClaims = oldToken.Claims.ToList();

        var newToken = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: newClaims,
            expires: time,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(newToken);
    }

    public string CreateAccessToken(User user)
    {
        _logger.LogInformation("creating access token");

        return CreateToken(user, DateTime.UtcNow.AddMinutes(1));
    }

    public string RefreshAccessToken(string token)
    {
        _logger.LogInformation("refreshing acces token");

        return RefreshToken(token, DateTime.UtcNow.AddMinutes(1));
    }

    public string CreateRefreshToken(User user)
    {
        _logger.LogInformation("creating refresh token");

        return CreateToken(user, DateTime.UtcNow.AddMinutes(3));
    }

    public bool IsTokenValid(string token, out Exception? ex)
    {
        try
        {
            JWTHandler.ValidateToken(token, GetTokenValidationsParameters(), out var _);
            ex = null;
            return true;
        }
        catch (SecurityTokenExpiredException)
        {
            ex = new SecurityTokenExpiredException();
            return false;
        }
        catch (Exception)
        {
            ex = null;
            return false;
        }
    }

    public IEnumerable<Claim> GetClaim(string token)
    {
        return JWTHandler.ReadJwtToken(token).Claims;
    }

    public TokenValidationParameters GetTokenValidationsParameters()
    {
        return new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidIssuer = _config["Issuer"],
            ValidateAudience = true,
            ValidAudience = _config["Audience"],
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Key"]!)),
            ValidateIssuerSigningKey = true,
        };
    }
}