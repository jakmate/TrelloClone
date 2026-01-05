using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using TrelloClone.Server.Application.Services;
using TrelloClone.Shared.DTOs.Auth;
using TrelloClone.Shared.DTOs.User;

namespace TrelloClone.Server.Controllers;

[ApiController]
[Route("api/auth")]
public partial class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _authService.LoginAsync(req);
            Log.UserLoggedIn(_logger, req.Email);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            Log.FailedLogin(_logger, req.Email, ex.Message);
            return Unauthorized(new { message = "Invalid credentials" });
        }
        catch (Exception ex)
        {
            Log.LoginError(_logger, ex, req.Email);
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _authService.RegisterAsync(req);
            Log.UserRegistered(_logger, req.Email);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            Log.RegistrationFailed(_logger, req.Email, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.RegistrationError(_logger, ex, req.Email);
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.RefreshTokenError(_logger, ex);
            return StatusCode(500, new { message = "An error occurred during token refresh" });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        try
        {
            await _authService.LogoutAsync(request.RefreshToken);
            return Ok();
        }
        catch (Exception ex)
        {
            Log.LogoutError(_logger, ex);
            return StatusCode(500, new { message = "An error occurred during logout" });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserResponse>> GetCurrentUser()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var response = await _authService.GetCurrentUserAsync(userId);
            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "User not found" });
        }
        catch (Exception ex)
        {
            Log.GetCurrentUserError(_logger, ex);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    [HttpPost("validate")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult ValidateToken()
    {
        // If we get here, the token is valid (passed through [Authorize])
        return Ok(new { valid = true });
    }

    [HttpPut("update-user")]
    [Authorize]
    public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request)
    {
        var userId = GetUserIdFromClaims();
        try
        {
            var updatedUser = await _authService.UpdateUserAsync(userId, request);
            return Ok(updatedUser);
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.UpdateUserError(_logger, ex, userId);
            return StatusCode(500, new { message = "Failed to update user" });
        }
    }

    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetUserIdFromClaims();
        try
        {
            await _authService.ChangePasswordAsync(userId, request);
            return Ok();
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ChangePasswordError(_logger, ex, userId);
            return StatusCode(500, new { message = "Failed to change password" });
        }
    }

    [HttpPost("delete-account")]
    [Authorize]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
    {
        var userId = GetUserIdFromClaims();
        try
        {
            await _authService.DeleteAccountAsync(userId, request);
            return Ok();
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.DeleteAccountError(_logger, ex, userId);
            return StatusCode(500, new { message = "Failed to delete account" });
        }
    }

    private Guid GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user identity");
        }

        return userId;
    }

    [HttpGet("check-username/{username}")]
    public async Task<ActionResult> CheckUsername(string username)
    {
        var exists = await _authService.CheckUsernameExistsAsync(username);
        return Ok(new { isAvailable = !exists });
    }

    [HttpGet("check-email/{email}")]
    public async Task<ActionResult> CheckEmail(string email)
    {
        var exists = await _authService.CheckEmailExistsAsync(email);
        return Ok(new { isAvailable = !exists });
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "User {Email} logged in successfully")]
        public static partial void UserLoggedIn(ILogger logger, string email);

        [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Failed login attempt for {Email}: {Message}")]
        public static partial void FailedLogin(ILogger logger, string email, string message);

        [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Error during login for {Email}")]
        public static partial void LoginError(ILogger logger, Exception exception, string email);

        [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "User {Email} registered successfully")]
        public static partial void UserRegistered(ILogger logger, string email);

        [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Registration failed for {Email}: {Message}")]
        public static partial void RegistrationFailed(ILogger logger, string email, string message);

        [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Error during registration for {Email}")]
        public static partial void RegistrationError(ILogger logger, Exception exception, string email);

        [LoggerMessage(EventId = 7, Level = LogLevel.Error, Message = "Error getting current user")]
        public static partial void GetCurrentUserError(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 8, Level = LogLevel.Error, Message = "Error updating user {UserId}")]
        public static partial void UpdateUserError(ILogger logger, Exception exception, Guid userId);

        [LoggerMessage(EventId = 9, Level = LogLevel.Error, Message = "Error changing password for user {UserId}")]
        public static partial void ChangePasswordError(ILogger logger, Exception exception, Guid userId);

        [LoggerMessage(EventId = 10, Level = LogLevel.Error, Message = "Error deleting account for user {UserId}")]
        public static partial void DeleteAccountError(ILogger logger, Exception exception, Guid userId);

        [LoggerMessage(EventId = 11, Level = LogLevel.Error, Message = "Error refreshing token")]
        public static partial void RefreshTokenError(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 12, Level = LogLevel.Error, Message = "Error during logout")]
        public static partial void LogoutError(ILogger logger, Exception exception);
    }
}
