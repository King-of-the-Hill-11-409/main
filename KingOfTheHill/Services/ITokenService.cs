using KingOfTheHill.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;
using System.Net;

namespace KingOfTheHill.Services
{
    public interface ITokenService
    {
        Task<string> GetTokenAsync();
        Task<string> RefreshTokenASync();
    }

    public class TokenService : ITokenService
    {
        private HttpClient _httpClient;
        private IHttpContextAccessor _httpContextAccessor;
        private IConfiguration configuration;
        private readonly ILogger _logger;

        public TokenService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ILogger logger)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            this.configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GetTokenAsync()
        {
            _logger.LogInformation("Getting token");

            return await _httpContextAccessor!.HttpContext!.GetTokenAsync("access_token")
                ?? throw new InvalidOperationException("Token was not found");
        }

        public async Task<string> RefreshTokenASync()
        {
            _logger.LogInformation("Refreshing acces token");

            var refreshToken = await _httpContextAccessor!.HttpContext!.GetTokenAsync("refreshToken");

            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogInformation("refresh token is null or empty");

                throw new UnauthorizedAccessException();
            }

            var response = await _httpClient.PostAsJsonAsync("api/Login/Refresh", new { refreshToken });

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogInformation("refresh token is not valid");

                throw new UnauthorizedAccessException();
            }

            var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();

            return result!.AccesToken;
        }
    }
}
