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
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json");
var jwtSettings = builder.Configuration.GetSection("Jwt");

builder.Services.AddHttpClient();
builder.Services.AddControllers();

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

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["token"];

                if (!string.IsNullOrEmpty(token))
                    context.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

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

builder.Services.AddLogging();
builder.Services.AddTransient<ILogger, Logger<string>>();
builder.Services.AddSingleton<IGameProvider, GameProvider>();
builder.Services.AddSingleton<JwtTokenFactory>();
builder.Services.AddTransient<GameTimerService>();

var app = builder.Build();

app.MapControllerRoute(
    "login",
    "/{action=Login}",
    defaults: new { Controller = "Login" }
    );

app.UseHttpsRedirection()
   .UseStaticFiles()
   .UseAntiforgery()
   .UseAuthentication()
   .UseAuthorization();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.MapHub<MainHub>("/gamehub");

app.MapGet("/api/secret", () => "Только для авторизованных!")
   .RequireAuthorization();

app.Run();