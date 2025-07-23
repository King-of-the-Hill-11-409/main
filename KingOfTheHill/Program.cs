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
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json");
var jwtSettings = builder.Configuration.GetSection("Jwt");

builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Path = "/"; // Важно для единообрадия!
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.Cookie.Name = "auth_cookie";
    options.Cookie.SameSite = SameSiteMode.None;

    options.Events = new CookieAuthenticationEvents
    {
        OnValidatePrincipal = async context =>
        {
            var accessToken = context.Properties.GetTokenValue("access_token");
            if (string.IsNullOrEmpty(accessToken))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
            }
        },
    };
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["Key"]!)),
        ValidateIssuerSigningKey = true,
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Для SignalR через query string
            if (context.Request.Path.StartsWithSegments("/hub") &&
                context.Request.Query.TryGetValue("token", out var token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        },

        OnAuthenticationFailed = context =>
        {
            if (context.Exception is SecurityTokenExpiredException)
            {
                context.Response.Headers.Append("Token-Expired", "true");
            }
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
builder.Services.AddHttpContextAccessor();
builder.Services.AddLogging();
builder.Services.AddSingleton<IHubConnectionBuilder, HubConnectionBuilder>();
builder.Services.AddTransient<ILogger, Logger<string>>();
builder.Services.AddSingleton<IGameProvider, GameProvider>();
builder.Services.AddSingleton<JwtTokenFactory>();
builder.Services.AddTransient<GameTimerService>();
builder.Services.AddScoped<SignalRConnectionManager>();
builder.Services.AddTransient<ITokenService, TokenService>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    await next();

    if (context.Response.Headers.ContainsKey("Token-Expired"))
    {
        context.Response.Headers.Remove("Token-Expired");
        var tokenService = context.RequestServices.GetRequiredService<ITokenService>();
        
        try
        {
            await tokenService.RefreshTokenASync();
            context.Response.Redirect(context.Request.Path);
        }
        catch
        {
            context.Response.Redirect("/");
        }

    }
});

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