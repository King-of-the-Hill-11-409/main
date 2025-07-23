using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace KingOfTheHill.Services
{
    public class SignalRConnectionManager : ComponentBase, IAsyncDisposable
    {
        [Inject] ITokenService _tokenService { get; set; } = null!;
        [Inject] NavigationManager Navigation { get; set; } = null!;
        HubConnection _hubConnection { get; set; } = null!;


        protected override async Task OnInitializedAsync()
        {
            if (_hubConnection is null) Build();

            _hubConnection!.Closed += async (error) =>
            {
                if (error is HttpRequestException http &&
                http.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    try
                    {
                        await _tokenService.RefreshTokenASync();
                        await _hubConnection.StartAsync();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Navigation.NavigateTo("/");
                    }
                }
            };

            await base.OnInitializedAsync();
        }

        public HubConnection Build()
        {
            var _hubConnectionBuider = new HubConnectionBuilder();

            return _hubConnection = _hubConnectionBuider
                .WithUrl(Navigation.ToAbsoluteUri("/gamehub"), options =>

                options.AccessTokenProvider = async () =>
                {
                    try
                    {
                        return await _tokenService.GetTokenAsync();
                    }
                    catch (SecurityTokenException)
                    {
                        return await _tokenService.RefreshTokenASync();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return null;
                    }

                })
                .AddNewtonsoftJsonProtocol(options =>
                {
                    options.PayloadSerializerSettings = new JsonSerializerSettings
                    {

                        NullValueHandling = NullValueHandling.Ignore,
                        Formatting = Formatting.None,
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                        TypeNameHandling = TypeNameHandling.Auto,
                        ContractResolver = new DefaultContractResolver
                        {
                            NamingStrategy = new CamelCaseNamingStrategy()
                        }
                    };
                })
                .WithAutomaticReconnect()
                .Build();
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection is not null) await _hubConnection.DisposeAsync();
        }
    }
}
