using KingOfTheHill.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;

namespace KingOfTheHill.Attributes
{
    public class AutoRefreshTokenFilter : IHubFilter
    {
        private readonly JwtTokenFactory _tokenService;
        public AutoRefreshTokenFilter(JwtTokenFactory tokenService)
        {
            _tokenService = tokenService;
        }

        public async ValueTask<object?> InvokeMethodAsync(
            HubInvocationContext invocationContext,
            Func<HubInvocationContext, ValueTask<object?>> next)
        {
            var context = invocationContext.Context;
            var hub = invocationContext.Hub;
            var httpContext = context.GetHttpContext();
            var token = httpContext?.Request.Query["token"].FirstOrDefault();

            if (string.IsNullOrEmpty(token))
            {
                await hub.Clients.Caller.SendAsync("401");

                throw new HubException("Invalid token");
            }


            if (!_tokenService.IsTokenValid(token, out var ex))
            {
                if (ex is SecurityTokenExpiredException)
                {
                    var newToken = _tokenService.RefreshToken(token);
                    await hub.Clients.Caller.SendAsync("ReceiveRefreshedToken", newToken);

                    httpContext!.Items["RefreshedToken"] = newToken;
                }
                else
                {
                    await hub.Clients.Caller.SendAsync("401");
                    throw new HubException("Invalid token");
                }
            }

            return await next(invocationContext);
        }
    }
}
