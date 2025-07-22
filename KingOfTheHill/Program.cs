using KingOfTheHill;
using KingOfTheHill.Components;
using KingOfTheHill.Hubs;
using KingOfTheHill.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KingOfTheHill.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Конфигурация JWT
builder.Configuration.AddJsonFile("appsettings.json");
var jwtSettings = builder.Configuration.GetSection("Jwt");

// 2. Настройка аутентификации
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Key"]!)),
            ValidateIssuerSigningKey = true,
        };

        // Для SignalR через WebSocket
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token))
                    context.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// 3. Сервисы приложения
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR()
    .AddNewtonsoftJsonProtocol(opts =>
    {
        opts.PayloadSerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
    });

builder.Services.AddSingleton<IGameProvider, GameProvider>();
builder.Services.AddTransient<GameTimerService>();

// 4. Сборка приложения
var app = builder.Build();

// 5. Middleware pipeline
app.UseHttpsRedirection()
   .UseStaticFiles()
   .UseAntiforgery()
   .UseAuthentication()  // Важно: до UseAuthorization!
   .UseAuthorization();

// 6. Маршруты
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.MapHub<MainHub>("/gamehub");

// 7. JWT-эндпоинты
app.MapPost("/api/login", (LoginRequest request, JwtTokenFactory tokenFactory) =>
{
    var user = new User { Name = request.Username };
    var token = tokenFactory.CreateToken(user);

    return Results.Ok(new LoginResponse
    {
        Token = token
    });
});

app.MapGet("/api/secret", () => "Только для авторизованных!")
   .RequireAuthorization();

app.Run();