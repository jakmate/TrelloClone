using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Moq;

using TrelloClone.Server.Application.Interfaces;
using TrelloClone.Server.Controllers;
using TrelloClone.Shared.DTOs.Auth;
using TrelloClone.Shared.DTOs.User;

using Xunit;

namespace TrelloClone.Server.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _mockService;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockService = new Mock<IAuthService>();
        _mockLogger = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(_mockService.Object, _mockLogger.Object);
    }

    private void SetupUser(string userId)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOk()
    {
        // Arrange
        var response = new AuthResponse { Token = "token" };
        _mockService.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>())).ReturnsAsync(response);

        // Act
        var result = await _controller.Login(new LoginRequest { Email = "test@test.com" });

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        _mockService.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.Login(new LoginRequest { Email = "test@test.com" });

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_Exception_Returns500()
    {
        // Arrange
        _mockService.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ThrowsAsync(new Exception());

        // Act
        var result = await _controller.Login(new LoginRequest { Email = "test@test.com" });

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsOk()
    {
        // Arrange
        var response = new AuthResponse { Token = "token" };
        _mockService.Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>())).ReturnsAsync(response);

        // Act
        var result = await _controller.Register(new RegisterRequest { Email = "test@test.com" });

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task Register_InvalidOperation_ReturnsBadRequest()
    {
        // Arrange
        _mockService.Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>()))
            .ThrowsAsync(new InvalidOperationException("Error"));

        // Act
        var result = await _controller.Register(new RegisterRequest { Email = "test@test.com" });

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Register_Exception_Returns500()
    {
        // Arrange
        _mockService.Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>()))
            .ThrowsAsync(new Exception());

        // Act
        var result = await _controller.Register(new RegisterRequest { Email = "test@test.com" });

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task Refresh_ValidToken_ReturnsOk()
    {
        // Arrange
        var response = new AuthResponse { Token = "new-token" };
        _mockService.Setup(x => x.RefreshTokenAsync(It.IsAny<RefreshTokenRequest>())).ReturnsAsync(response);

        // Act
        var result = await _controller.Refresh(new RefreshTokenRequest());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task Refresh_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        _mockService.Setup(x => x.RefreshTokenAsync(It.IsAny<RefreshTokenRequest>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.Refresh(new RefreshTokenRequest());

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Logout_ValidRequest_ReturnsOk()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());

        // Act
        var result = await _controller.Logout(new RefreshTokenRequest());

        // Assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task Logout_Exception_Returns500()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.LogoutAsync(It.IsAny<string>())).ThrowsAsync(new Exception());

        // Act
        var result = await _controller.Logout(new RefreshTokenRequest());

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        SetupUser("invalid");

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetCurrentUser_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.GetCurrentUserAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new KeyNotFoundException());

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public void ValidateToken_ValidToken_ReturnsOk()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());

        // Act
        var result = _controller.ValidateToken();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateUser_ValidRequest_ReturnsOk()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        var authResponse = new AuthResponse
        {
            Token = "token",
            User = new UserDto { Id = Guid.NewGuid() }
        };
        _mockService.Setup(x => x.UpdateUserAsync(It.IsAny<Guid>(), It.IsAny<UpdateUserRequest>()))
            .ReturnsAsync(authResponse);

        // Act
        var result = await _controller.UpdateUser(new UpdateUserRequest());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(authResponse, okResult.Value);
    }

    [Fact]
    public async Task UpdateUser_UnauthorizedAccess_ReturnsBadRequest()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.UpdateUserAsync(It.IsAny<Guid>(), It.IsAny<UpdateUserRequest>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.UpdateUser(new UpdateUserRequest());

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ChangePassword_ValidRequest_ReturnsOk()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());

        // Act
        var result = await _controller.ChangePassword(new ChangePasswordRequest());

        // Assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task ChangePassword_InvalidPassword_ReturnsBadRequest()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.ChangePasswordAsync(It.IsAny<Guid>(), It.IsAny<ChangePasswordRequest>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.ChangePassword(new ChangePasswordRequest());

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteAccount_ValidRequest_ReturnsOk()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());

        // Act
        var result = await _controller.DeleteAccount(new DeleteAccountRequest());

        // Assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task DeleteAccount_UnauthorizedAccess_ReturnsBadRequest()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.DeleteAccountAsync(It.IsAny<Guid>(), It.IsAny<DeleteAccountRequest>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.DeleteAccount(new DeleteAccountRequest());

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CheckUsername_ReturnsAvailability()
    {
        // Arrange
        _mockService.Setup(x => x.CheckUsernameExistsAsync("test")).ReturnsAsync(false);

        // Act
        var result = await _controller.CheckUsername("test");

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CheckEmail_ReturnsAvailability()
    {
        // Arrange
        _mockService.Setup(x => x.CheckEmailExistsAsync("test@test.com")).ReturnsAsync(true);

        // Act
        var result = await _controller.CheckEmail("test@test.com");

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Login_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("Email", "Required");
        var request = new LoginRequest { Email = "", Password = "" };

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Register_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("Email", "Required");
        var request = new RegisterRequest { Email = "", Password = "" };

        // Act
        var result = await _controller.Register(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Refresh_Exception_Returns500()
    {
        // Arrange
        _mockService.Setup(x => x.RefreshTokenAsync(It.IsAny<RefreshTokenRequest>()))
            .ThrowsAsync(new Exception("An error occurred during token refresh"));

        // Act
        var result = await _controller.Refresh(new RefreshTokenRequest());

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_Exception_Returns500()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.GetCurrentUserAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("An error occurred"));

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_Exception_Returns500()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.UpdateUserAsync(It.IsAny<Guid>(), It.IsAny<UpdateUserRequest>()))
            .ThrowsAsync(new Exception("Failed to update user"));

        // Act
        var result = await _controller.UpdateUser(new UpdateUserRequest());

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_Exception_Returns500()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.ChangePasswordAsync(It.IsAny<Guid>(), It.IsAny<ChangePasswordRequest>()))
            .ThrowsAsync(new Exception("Failed to change password"));

        // Act
        var result = await _controller.ChangePassword(new ChangePasswordRequest());

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_Exception_Returns500()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.DeleteAccountAsync(It.IsAny<Guid>(), It.IsAny<DeleteAccountRequest>()))
            .ThrowsAsync(new Exception("Failed to delete account"));

        // Act
        var result = await _controller.DeleteAccount(new DeleteAccountRequest());

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_InvalidOperation_ReturnsBadRequest()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.UpdateUserAsync(It.IsAny<Guid>(), It.IsAny<UpdateUserRequest>()))
            .ThrowsAsync(new InvalidOperationException("Invalid operation"));

        // Act
        var result = await _controller.UpdateUser(new UpdateUserRequest());

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ChangePassword_InvalidOperation_ReturnsBadRequest()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.ChangePasswordAsync(It.IsAny<Guid>(), It.IsAny<ChangePasswordRequest>()))
            .ThrowsAsync(new InvalidOperationException("Invalid operation"));

        // Act
        var result = await _controller.ChangePassword(new ChangePasswordRequest());

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetCurrentUser_InvalidUserIdClaim_ReturnsUnauthorized()
    {
        // Arrange
        SetupUser("not-a-guid");

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetCurrentUser_ValidRequest_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUser(userId.ToString());
        var userDto = new UserDto { Id = userId };
        var response = new CurrentUserResponse { User = userDto };
        _mockService.Setup(x => x.GetCurrentUserAsync(userId)).ReturnsAsync(response);

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(response, okResult.Value);
    }
}
