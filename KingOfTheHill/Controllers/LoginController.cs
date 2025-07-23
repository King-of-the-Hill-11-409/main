using KingOfTheHill.Models;
using KingOfTheHill.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.JSInterop;
using Newtonsoft.Json.Linq;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace KingOfTheHill.Controllers
{

    [Route("api/Login")]
    public class LoginController : ControllerBase
    {
        JwtSecurityTokenHandler jWThandler = new JwtSecurityTokenHandler();
        private readonly ILogger _logger;
        private readonly IJSRuntime _jSRuntime;
        private readonly JwtTokenFactory _tokenFactory;
        public LoginController(ILogger logger, JwtTokenFactory tokenFactory, IJSRuntime jSRuntime)
        {
            _logger = logger;
            _tokenFactory = tokenFactory;
            _jSRuntime = jSRuntime;
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = new User() { Name = request.Username };

            var accessToken = _tokenFactory.CreateAccessToken(user);
            var refreshToken = _tokenFactory.CreateRefreshToken(user);

            var claims = jWThandler.ReadJwtToken(accessToken).Claims;

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                IsPersistent = true,
                AllowRefresh = true
            };

            authProperties.StoreTokens(new[]
{
                new AuthenticationToken { Name = "accessToken", Value = accessToken },
                new AuthenticationToken { Name = "refreshToken", Value = refreshToken }
                    });

            await HttpContext.SignInAsync(
                  CookieAuthenticationDefaults.AuthenticationScheme,
                  principal,
                  authProperties);

            _logger.LogInformation($"Token {accessToken} was created for user: {user.Name}, Id: {user.Id}");

            return Ok(new LoginResponse()
            {
                AccesToken = accessToken,
                RefreshToken = refreshToken
            });
                
        }

        [HttpPost("Refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var refreshToken = request.RefreshToken;

            try
            {
                _logger.LogInformation("trying to refresh access token");

                if (!_tokenFactory.IsTokenValid(refreshToken, out Exception? ex))
                {
                    return Unauthorized(new { Message = "Unauthorized", RedirectToLogin = true });
                }

                var accessToken = _tokenFactory.RefreshAccessToken(refreshToken);
                var authProp = await HttpContext.AuthenticateAsync();

                var updateResult = authProp!.Properties!.UpdateTokenValue("accessToken", accessToken);

                if (!updateResult) throw new Exception();

                var claims = jWThandler.ReadJwtToken(accessToken).Claims;
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                var authProperties = new AuthenticationProperties
                {
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                    IsPersistent = true,
                    AllowRefresh = true
                };

                await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);

                return Ok(new RefreshTokenResponse()
                {
                    AccesToken = accessToken,
                    RefreshToken = refreshToken
                });

            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogInformation($"Refresh token {refreshToken} is expired");

                return Unauthorized(new { Message = "Unauthorized", RedirectToLogin = true });
            }
            catch (Exception)
            {
                _logger.LogError("Internal server error");

                return StatusCode(500);
            }
        }
    }
}
