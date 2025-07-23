using KingOfTheHill.Models;
using KingOfTheHill.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.JSInterop;
using Newtonsoft.Json.Linq;

namespace KingOfTheHill.Controllers
{

    [Route("api/Login")]
    public class LoginController : ControllerBase
    {

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
        public LoginResponse Login([FromBody] LoginRequest request)
        {
            var user = new User() { Name = request.Username };

            var accessToken = _tokenFactory.CreateAccessToken(user);
            var refreshToken = _tokenFactory.CreateRefreshToken(user);

            var response = new LoginResponse();

            response.AccesToken = accessToken;
            response.RefreshToken = refreshToken;

            _logger.LogInformation($"Token {accessToken} was created for user: {user.Name}, Id: {user.Id}");

            return response;
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

                await _jSRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", accessToken);

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
