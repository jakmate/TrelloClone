using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrelloClone.Shared.DTOs;

namespace TrelloClone.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
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
            _logger.LogInformation("User {Email} logged in successfully", req.Email);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Failed login attempt for {Email}: {Message}", req.Email, ex.Message);
            return Unauthorized(new { message = "Invalid credentials" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", req.Email);
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
            _logger.LogInformation("User {Email} registered successfully", req.Email);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Registration failed for {Email}: {Message}", req.Email, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", req.Email);
            return StatusCode(500, new { message = "An error occurred during registration" });
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
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    [HttpPost("validate")]
    [Authorize]
    public IActionResult ValidateToken()
    {
        // If we get here, the token is valid (passed through [Authorize])
        return Ok(new { valid = true });
    }
}