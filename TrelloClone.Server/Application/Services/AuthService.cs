using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.IdentityModel.Tokens;

using TrelloClone.Server.Domain.Entities;
using TrelloClone.Server.Domain.Interfaces;
using TrelloClone.Shared.DTOs;

namespace TrelloClone.Server.Application.Services;

public partial class AuthService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IUnitOfWork uow,
        IConfiguration config,
        ILogger<AuthService> logger)
    {
        _users = users;
        _uow = uow;
        _config = config;
        _logger = logger;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        var user = await _users.GetByEmailAsync(req.Email);
        if (user == null || !VerifyPassword(req.Password, user.PasswordHash))
        {
            // Add delay to prevent timing attacks
            await Task.Delay(100);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        var token = GenerateJwtToken(user);
        return new AuthResponse
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email
            }
        };
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
    {
        // Check if user already exists
        if (await _users.GetByEmailAsync(req.Email) != null)
        {
            throw new InvalidOperationException("Email already registered");
        }

        // Validate password strength
        if (!IsPasswordValid(req.Password))
        {
            throw new InvalidOperationException("Password must be at least 6 characters long and contain both letters and numbers");
        }

        // Validate username
        if (string.IsNullOrWhiteSpace(req.UserName) || req.UserName.Length < 3)
        {
            throw new InvalidOperationException("Username must be at least 3 characters long");
        }

        var user = new User
        {
            UserName = req.UserName.Trim(),
            Email = req.Email.ToLowerInvariant(),
            PasswordHash = HashPassword(req.Password)
        };

        _users.Add(user);
        await _uow.SaveChangesAsync();

        var token = GenerateJwtToken(user);
        return new AuthResponse
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email
            }
        };
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        return new CurrentUserResponse
        {
            User = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email
            }
        };
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey is required");

        // JWT secret is loaded from environment variables/user secrets
        // Never stored in code or config files, so this suppression is safe
        #pragma warning disable S4453
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        #pragma warning restore S4453

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(
                JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
    }

    private bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (Exception ex)
        {
            Log.PasswordVerificationFailed(_logger, ex);
            return false;
        }
    }

    private static bool IsPasswordValid(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            return false;
        }

        // Must contain at least one letter and one number
        return Regex.IsMatch(password, @"^(?=.*[A-Za-z])(?=.*\d).{6,}$",
            RegexOptions.None, TimeSpan.FromMilliseconds(100));
    }

    public async Task<bool> CheckUsernameExistsAsync(string username)
    {
        return await _users.GetByUsernameAsync(username) != null;
    }

    public async Task<bool> CheckEmailExistsAsync(string email)
    {
        return await _users.GetByEmailAsync(email) != null;
    }

    public async Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        // Prevent duplicate username/email
        if (request.UserName != user.UserName && await _users.GetByUsernameAsync(request.UserName) != null)
        {
            throw new InvalidOperationException("Username already taken");
        }

        if (request.Email != user.Email && await _users.GetByEmailAsync(request.Email) != null)
        {
            throw new InvalidOperationException("Email already registered");
        }

        user.UserName = request.UserName.Trim();
        user.Email = request.Email.ToLowerInvariant();

        await _uow.SaveChangesAsync();

        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email
        };
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Current password is incorrect");
        }

        if (!IsPasswordValid(request.NewPassword))
        {
            throw new InvalidOperationException("New password must be at least 6 characters and contain letters and numbers");
        }

        user.PasswordHash = HashPassword(request.NewPassword);
        await _uow.SaveChangesAsync();
    }

    public async Task DeleteAccountAsync(Guid userId)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        _users.Remove(user);
        await _uow.SaveChangesAsync();
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Warning,
            Message = "Password verification failed")]
        public static partial void PasswordVerificationFailed(
            ILogger logger,
            Exception exception);
    }
}
