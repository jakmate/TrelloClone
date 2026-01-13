using System.Security.Claims;
using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using TrelloClone.Server.Application.Hubs;
using TrelloClone.Server.Application.Interfaces;
using TrelloClone.Server.Application.Services;
using TrelloClone.Server.Domain.Interfaces;
using TrelloClone.Server.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

SetupConfiguration(builder);
var configuration = builder.Configuration;

ValidateConfiguration(builder, configuration);
ConfigureServices(builder, configuration);

var app = builder.Build();

ConfigurePipeline(app);
await app.RunAsync();

static void ValidateConfiguration(WebApplicationBuilder builder, IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("Default");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException(
            builder.Environment.IsDevelopment()
                ? "Database connection string is required. Set it in User Secrets: dotnet user-secrets set \"ConnectionStrings:Default\" \"your-connection-string\""
                : "Database connection string is required. Set it as an environment variable named 'ConnectionStrings__Default'");
    }

    var jwtSettings = configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"];

    if (string.IsNullOrEmpty(secretKey))
    {
        throw new InvalidOperationException(
            builder.Environment.IsDevelopment()
                ? "JWT SecretKey is required. Set it in User Secrets: dotnet user-secrets set \"JwtSettings:SecretKey\" \"your-secure-key\""
                : "JWT SecretKey is required. Set it as an environment variable named 'JwtSettings__SecretKey'");
    }

    if (string.IsNullOrEmpty(jwtSettings["Issuer"]) || string.IsNullOrEmpty(jwtSettings["Audience"]))
    {
        throw new InvalidOperationException("JWT Issuer and Audience are required in configuration.");
    }
}

static void ConfigureServices(WebApplicationBuilder builder, IConfiguration configuration)
{
    builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    ConfigureDatabase(builder);
    ConfigureAuthentication(builder, configuration);

    builder.Services.AddAuthorization();
    RegisterRepositories(builder.Services);
    RegisterApplicationServices(builder.Services);

    builder.Services.AddSignalR();
    builder.Services.AddControllers();

    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();

    ConfigureKestrel(builder);
    ConfigureCors(builder, configuration);
}

static void ConfigureDatabase(WebApplicationBuilder builder)
{
    var connectionString = builder.Configuration.GetConnectionString("Default")!;

    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));
    }
    else
    {
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString,
                sqlOptions => sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null)));
    }
}

static void ConfigureAuthentication(WebApplicationBuilder builder, IConfiguration configuration)
{
    var jwtSettings = configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"]!;
    var issuer = jwtSettings["Issuer"]!;
    var audience = jwtSettings["Audience"]!;

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/boardhub"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<Program>>();
                    Log.AuthenticationFailed(logger, context.Exception);
                    return Task.CompletedTask;
                }
            };
        });
}

static void RegisterRepositories(IServiceCollection services)
{
    services.AddScoped<IUnitOfWork, UnitOfWork>();
    services.AddScoped<IBoardRepository, BoardRepository>();
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IBoardUserRepository, BoardUserRepository>();
    services.AddScoped<IColumnRepository, ColumnRepository>();
    services.AddScoped<ITaskRepository, TaskRepository>();
    services.AddScoped<IBoardInvitationRepository, BoardInvitationRepository>();
}

static void RegisterApplicationServices(IServiceCollection services)
{
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<IBoardService, BoardService>();
    services.AddScoped<IUserService, UserService>();
    services.AddScoped<IColumnService, ColumnService>();
    services.AddScoped<ITaskService, TaskService>();
    services.AddScoped<IInvitationService, InvitationService>();
}

static void ConfigureKestrel(WebApplicationBuilder builder)
{
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.ConfigureHttpsDefaults(httpsOptions =>
        {
            httpsOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;
        });
    });
}

static void ConfigureCors(WebApplicationBuilder builder, IConfiguration configuration)
{
    var allowedOrigins = configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>()
        ?? throw new InvalidOperationException("CORS AllowedOrigins configuration is required");

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowClient", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
        });
    });
}

static void ConfigurePipeline(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    else
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    AddSecurityHeaders(app);

    app.UseCors("AllowClient");
    app.UseAuthentication();
    UseSessionValidation(app);
    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<BoardHub>("/boardhub");
}

static void AddSecurityHeaders(WebApplication app)
{
    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

        if (!app.Environment.IsDevelopment())
        {
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; connect-src 'self' wss:; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'; upgrade-insecure-requests";
        }

        await next();
    });
}

static void UseSessionValidation(WebApplication app)
{
    app.Use(async (context, next) =>
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var sessionId = context.User.FindFirst("session_id")?.Value;
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (sessionId != null && userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                var refreshTokenService = context.RequestServices.GetRequiredService<IRefreshTokenService>();
                var activeSession = await refreshTokenService.GetActiveSessionAsync(userId);
                if (activeSession != sessionId)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Session expired");
                    return;
                }
            }
        }
        await next();
    });
}

static void SetupConfiguration(WebApplicationBuilder builder)
{
    builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    builder.Configuration.AddJsonFile(
        $"appsettings.{builder.Environment.EnvironmentName}.json",
        optional: true,
        reloadOnChange: true);

    if (builder.Environment.IsDevelopment())
    {
        builder.Configuration.AddUserSecrets<Program>();
        Console.WriteLine("Using User Secrets for development environment");
    }
    else
    {
        builder.Configuration.AddEnvironmentVariables();
        Console.WriteLine("Using Environment Variables for production environment");
    }

    Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
}

public partial class Program
{
    private static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Authentication failed")]
        public static partial void AuthenticationFailed(ILogger logger, Exception exception);
    }
}
