using KingOfTheHill.Models;
using KingOfTheHill.Services;
using Microsoft.AspNetCore.Mvc;

namespace KingOfTheHill.Controllers
{

    public class LoginController : ControllerBase
    {

        private readonly ILogger _logger;
        private readonly JwtTokenFactory _tokenFactory;
        public LoginController(ILogger logger, JwtTokenFactory tokenFactory)
        {
            _logger = logger;
            _tokenFactory = tokenFactory;
        }

        [HttpPost]
        public LoginResponse Login([FromBody] LoginRequest request)
        {
            var user = new User() { Name = request.Username };

            var token = _tokenFactory.CreateToken(user);

            var response = new LoginResponse();

            response.Token = token;

            _logger.LogInformation($"Token {token} was created for user: {user.Name}, Id: {user.Id}");

            return response;
        }
    }
}
