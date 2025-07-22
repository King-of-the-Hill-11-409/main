using KingOfTheHill.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace KingOfTheHill.Services;

public class JwtTokenFactory
{
    private readonly IConfiguration _config;
    private JwtSecurityTokenHandler JWTHandler= new JwtSecurityTokenHandler();

    public JwtTokenFactory(IConfiguration config)
    {
        _config = config;
    }

    public string CreateToken(User user)
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
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string RefreshToken(string expiredToken)
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
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(newToken);
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