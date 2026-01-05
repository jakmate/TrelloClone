using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.IdentityModel.Tokens;

using TrelloClone.Server.Domain.Entities;
using TrelloClone.Server.Domain.Interfaces;
using TrelloClone.Shared.DTOs.Auth;
using TrelloClone.Shared.DTOs.User;

namespace TrelloClone.Server.Application.Services;

public partial class AuthService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;
    private readonly RefreshTokenService _refreshTokenService;

    public AuthService(
        IUserRepository users,
        IUnitOfWork uow,
        IConfiguration config,
        ILogger<AuthService> logger,
        RefreshTokenService refreshTokenService)
    {
        _users = users;
        _uow = uow;
        _config = config;
        _logger = logger;
        _refreshTokenService = refreshTokenService;
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

        // Invalidate all existing refresh tokens for this user
        await _refreshTokenService.RevokeAllUserTokensAsync(user.Id);

        // Generate a new session ID
        var sessionId = Guid.NewGuid().ToString();
        await _refreshTokenService.SetActiveSessionAsync(user.Id, sessionId);

        var token = GenerateJwtToken(user, sessionId);
        var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(user.Id, sessionId);

        return new AuthResponse
        {
            Token = token,
            RefreshToken = refreshToken,
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

        // Generate a new session ID
        var sessionId = Guid.NewGuid().ToString();
        await _refreshTokenService.SetActiveSessionAsync(user.Id, sessionId);

        var token = GenerateJwtToken(user, sessionId);
        var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(user.Id, sessionId);

        return new AuthResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            User = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email
            }
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest req)
    {
        var tokenInfo = await _refreshTokenService.ValidateRefreshTokenAsync(req.RefreshToken);
        if (tokenInfo == null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var user = await _users.GetByIdAsync(tokenInfo.UserId);
        if (user == null)
        {
            await _refreshTokenService.RevokeRefreshTokenAsync(req.RefreshToken);
            throw new UnauthorizedAccessException("User not found");
        }

        // Check if the session is still active
        var activeSession = await _refreshTokenService.GetActiveSessionAsync(user.Id);
        if (activeSession != tokenInfo.SessionId)
        {
            await _refreshTokenService.RevokeRefreshTokenAsync(req.RefreshToken);
            throw new UnauthorizedAccessException("Session expired");
        }

        // Generate new tokens
        var newToken = GenerateJwtToken(user, tokenInfo.SessionId);
        var newRefreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(
            user.Id, tokenInfo.SessionId);

        // Revoke the old refresh token
        await _refreshTokenService.RevokeRefreshTokenAsync(req.RefreshToken);

        return new AuthResponse
        {
            Token = newToken,
            RefreshToken = newRefreshToken,
            User = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email
            }
        };
    }

    // Update logout to revoke refresh token
    public async Task LogoutAsync(string refreshToken)
    {
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _refreshTokenService.RevokeRefreshTokenAsync(refreshToken);
        }
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

    private string GenerateJwtToken(User user, string sessionId)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey is required");

        // JWT secret is loaded from environment variables/user secrets
        // Remove sonarqube warning...
#pragma warning disable S6781
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
#pragma warning restore S6781

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("session_id", sessionId),
            new Claim(
                JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
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

        if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Current password is incorrect");
        }

        // Prevent duplicate username/email
        if (request.UserName != user.UserName)
        {
            var existingUserByUsername = await _users.GetByUsernameAsync(request.UserName);
            if (existingUserByUsername != null)
            {
                throw new InvalidOperationException("Username already taken");
            }
            user.UserName = request.UserName.Trim();
        }

        if (request.Email != user.Email)
        {
            var existingUserByEmail = await _users.GetByEmailAsync(request.Email);
            if (existingUserByEmail != null)
            {
                throw new InvalidOperationException("Email already registered");
            }
            user.Email = request.Email.ToLowerInvariant();
        }

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

    public async Task DeleteAccountAsync(Guid userId, DeleteAccountRequest request)
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
