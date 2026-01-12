using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Moq;

using TrelloClone.Server.Application.Interfaces;
using TrelloClone.Server.Application.Services;
using TrelloClone.Server.Domain.Entities;
using TrelloClone.Server.Domain.Interfaces;
using TrelloClone.Shared.DTOs.Auth;
using TrelloClone.Shared.DTOs.User;

using Xunit;

namespace TrelloClone.Server.Tests.Application;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockUsers;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly Mock<IRefreshTokenService> _mockRefreshToken;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _mockUsers = new Mock<IUserRepository>();
        _mockUow = new Mock<IUnitOfWork>();
        _mockConfig = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<AuthService>>();
        _mockRefreshToken = new Mock<IRefreshTokenService>();

        SetupConfiguration();
        _service = new AuthService(_mockUsers.Object, _mockUow.Object, _mockConfig.Object,
            _mockLogger.Object, _mockRefreshToken.Object);
    }

    private void SetupConfiguration()
    {
        var jwtSection = new Mock<IConfigurationSection>();
        jwtSection.Setup(x => x["SecretKey"]).Returns("ThisIsAVeryLongSecretKeyForTestingPurposesOnly123456");
        jwtSection.Setup(x => x["Issuer"]).Returns("TestIssuer");
        jwtSection.Setup(x => x["Audience"]).Returns("TestAudience");
        _mockConfig.Setup(x => x.GetSection("JwtSettings")).Returns(jwtSection.Object);
    }

    [Fact]
    public async Task LoginAsync_InvalidEmail_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _mockUsers.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.LoginAsync(new LoginRequest { Email = "test@test.com", Password = "pass" }));
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = new User
        {
            Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct")
        };
        _mockUsers.Setup(x => x.GetByEmailAsync("test@test.com")).ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.LoginAsync(new LoginRequest { Email = "test@test.com", Password = "wrong" }));
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            UserName = "test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
        };
        _mockUsers.Setup(x => x.GetByEmailAsync("test@test.com")).ReturnsAsync(user);
        _mockRefreshToken.Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync("refresh-token");

        // Act
        var result = await _service.LoginAsync(new LoginRequest
        {
            Email = "test@test.com",
            Password = "password123"
        });

        // Assert
        Assert.NotNull(result.Token);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.Equal(user.Id, result.User.Id);
    }

    [Fact]
    public async Task RegisterAsync_EmailExists_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockUsers.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(new User());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RegisterAsync(new RegisterRequest { Email = "test@test.com" }));
    }

    [Fact]
    public async Task RegisterAsync_WeakPassword_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockUsers.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RegisterAsync(new RegisterRequest
            {
                Email = "test@test.com",
                Password = "weak",
                UserName = "test"
            }));
    }

    [Fact]
    public async Task RegisterAsync_ShortUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockUsers.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RegisterAsync(new RegisterRequest
            {
                Email = "test@test.com",
                Password = "password123",
                UserName = "ab"
            }));
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_CreatesUser()
    {
        // Arrange
        _mockUsers.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _mockRefreshToken.Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync("refresh-token");

        // Act
        var result = await _service.RegisterAsync(new RegisterRequest
        {
            Email = "test@test.com",
            Password = "password123",
            UserName = "testuser"
        });

        // Assert
        _mockUsers.Verify(x => x.Add(It.IsAny<User>()), Times.Once);
        _mockUow.Verify(x => x.SaveChangesAsync(), Times.Once);
        Assert.NotNull(result.Token);
    }

    [Fact]
    public async Task RefreshTokenAsync_InvalidToken_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _mockRefreshToken.Setup(x => x.ValidateRefreshTokenAsync(It.IsAny<string>()))
            .ReturnsAsync((RefreshTokenInfo?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = "invalid" }));
    }

    [Fact]
    public async Task RefreshTokenAsync_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var tokenInfo = new RefreshTokenInfo { UserId = Guid.NewGuid(), SessionId = "session" };
        _mockRefreshToken.Setup(x => x.ValidateRefreshTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(tokenInfo);
        _mockUsers.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = "token" }));
    }

    [Fact]
    public async Task RefreshTokenAsync_SessionExpired_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var tokenInfo = new RefreshTokenInfo { UserId = Guid.NewGuid(), SessionId = "old-session" };
        _mockRefreshToken.Setup(x => x.ValidateRefreshTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(tokenInfo);
        _mockUsers.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User { Id = tokenInfo.UserId });
        _mockRefreshToken.Setup(x => x.GetActiveSessionAsync(It.IsAny<Guid>()))
            .ReturnsAsync("new-session");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = "token" }));
    }

    [Fact]
    public async Task GetCurrentUserAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockUsers.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.GetCurrentUserAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetCurrentUserAsync_ValidUser_ReturnsResponse()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), UserName = "test", Email = "test@test.com" };
        _mockUsers.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);

        // Act
        var result = await _service.GetCurrentUserAsync(user.Id);

        // Assert
        Assert.Equal(user.Id, result.User.Id);
        Assert.Equal(user.UserName, result.User.UserName);
    }

    [Fact]
    public async Task CheckUsernameExistsAsync_UsernameExists_ReturnsTrue()
    {
        // Arrange
        _mockUsers.Setup(x => x.GetByUsernameAsync("test")).ReturnsAsync(new User());

        // Act
        var result = await _service.CheckUsernameExistsAsync("test");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CheckEmailExistsAsync_EmailExists_ReturnsTrue()
    {
        // Arrange
        _mockUsers.Setup(x => x.GetByEmailAsync("test@test.com")).ReturnsAsync(new User());

        // Act
        var result = await _service.CheckEmailExistsAsync("test@test.com");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UpdateUserAsync_UpdateUsername_ReturnsUpdatedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "oldname",
            Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
        };
        _mockUsers.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockUsers.Setup(x => x.GetByUsernameAsync("newname")).ReturnsAsync((User?)null);

        // Act
        var result = await _service.UpdateUserAsync(userId, new UpdateUserRequest
        {
            CurrentPassword = "password123",
            UserName = "newname",
            Email = user.Email
        });

        // Assert
        Assert.Equal("newname", result.UserName);
        Assert.Equal(user.Email, result.Email);
        _mockUow.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_UpdateEmail_ReturnsUpdatedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            Email = "old@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
        };
        _mockUsers.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockUsers.Setup(x => x.GetByEmailAsync("new@test.com")).ReturnsAsync((User?)null);

        // Act
        var result = await _service.UpdateUserAsync(userId, new UpdateUserRequest
        {
            CurrentPassword = "password123",
            UserName = user.UserName,
            Email = "new@test.com"
        });

        // Assert
        Assert.Equal(user.UserName, result.UserName);
        Assert.Equal("new@test.com", result.Email);
        _mockUow.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_UsernameExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var user = new User
        {
            UserName = "oldname",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
        };
        _mockUsers.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);
        _mockUsers.Setup(x => x.GetByUsernameAsync("newname")).ReturnsAsync(new User());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateUserAsync(Guid.NewGuid(), new UpdateUserRequest
            {
                CurrentPassword = "password123",
                UserName = "newname",
                Email = user.Email ?? ""
            }));
    }

    [Fact]
    public async Task UpdateUserAsync_EmailExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var user = new User
        {
            Email = "old@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
        };
        _mockUsers.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);
        _mockUsers.Setup(x => x.GetByEmailAsync("new@test.com")).ReturnsAsync(new User());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateUserAsync(Guid.NewGuid(), new UpdateUserRequest
            {
                CurrentPassword = "password123",
                UserName = user.UserName,
                Email = "new@test.com"
            }));
    }

    [Fact]
    public async Task UpdateUserAsync_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = new User { PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct") };
        _mockUsers.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.UpdateUserAsync(Guid.NewGuid(), new UpdateUserRequest { CurrentPassword = "wrong" }));
    }

    [Fact]
    public async Task UpdateUserAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockUsers.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UpdateUserAsync(Guid.NewGuid(), new UpdateUserRequest { CurrentPassword = "password123" }));
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidRequest_UpdatesPassword()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
        };
        _mockUsers.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        await _service.ChangePasswordAsync(userId, new ChangePasswordRequest
        {
            CurrentPassword = "password123",
            NewPassword = "newpassword123"
        });

        // Assert
        _mockUsers.Verify(x => x.GetByIdAsync(userId), Times.Once);
        _mockUow.Verify(x => x.SaveChangesAsync(), Times.Once);
        Assert.NotEqual(user.PasswordHash, BCrypt.Net.BCrypt.HashPassword("password123"));
    }

    [Fact]
    public async Task ChangePasswordAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockUsers.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.ChangePasswordAsync(Guid.NewGuid(), new ChangePasswordRequest
            {
                CurrentPassword = "password123",
                NewPassword = "newpassword123"
            }));
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = new User { PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct") };
        _mockUsers.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.ChangePasswordAsync(Guid.NewGuid(), new ChangePasswordRequest
            {
                CurrentPassword = "wrong"
            }));
    }

    [Fact]
    public async Task ChangePasswordAsync_WeakNewPassword_ThrowsInvalidOperationException()
    {
        // Arrange
        var user = new User { PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123") };
        _mockUsers.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ChangePasswordAsync(Guid.NewGuid(), new ChangePasswordRequest
            {
                CurrentPassword = "password123",
                NewPassword = "weak"
            }));
    }

    [Fact]
    public async Task DeleteAccountAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockUsers.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.DeleteAccountAsync(Guid.NewGuid(), new DeleteAccountRequest
            {
                CurrentPassword = "password123"
            }));
    }

    [Fact]
    public async Task DeleteAccountAsync_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = new User { PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct") };
        _mockUsers.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.DeleteAccountAsync(Guid.NewGuid(), new DeleteAccountRequest
            {
                CurrentPassword = "wrong"
            }));
    }

    [Fact]
    public async Task DeleteAccountAsync_ValidRequest_RemovesUser()
    {
        // Arrange
        var user = new User { PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123") };
        _mockUsers.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);

        // Act
        await _service.DeleteAccountAsync(Guid.NewGuid(), new DeleteAccountRequest
        {
            CurrentPassword = "password123"
        });

        // Assert
        _mockUsers.Verify(x => x.Remove(user), Times.Once);
        _mockUow.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_RotatesRefreshToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldRefreshToken = "old-refresh-token";
        var newRefreshToken = "new-refresh-token";
        var sessionId = "session-id";

        var tokenInfo = new RefreshTokenInfo
        {
            UserId = userId,
            SessionId = sessionId,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            Email = "test@test.com"
        };

        _mockRefreshToken
            .Setup(x => x.ValidateRefreshTokenAsync(oldRefreshToken))
            .ReturnsAsync(tokenInfo);
        _mockUsers
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _mockRefreshToken
            .Setup(x => x.GetActiveSessionAsync(userId))
            .ReturnsAsync(sessionId);
        _mockRefreshToken
            .Setup(x => x.GenerateRefreshTokenAsync(userId, sessionId))
            .ReturnsAsync(newRefreshToken);

        // Act
        var result = await _service.RefreshTokenAsync(
            new RefreshTokenRequest { RefreshToken = oldRefreshToken }
        );

        // Assert
        Assert.NotNull(result.Token);
        Assert.Equal(newRefreshToken, result.RefreshToken);
        Assert.Equal(userId, result.User.Id);
        Assert.Equal(user.UserName, result.User.UserName);
        Assert.Equal(user.Email, result.User.Email);

        // Verify old token was revoked
        _mockRefreshToken.Verify(
            x => x.RevokeRefreshTokenAsync(oldRefreshToken),
            Times.Once
        );
        // Verify new token was generated
        _mockRefreshToken.Verify(
            x => x.GenerateRefreshTokenAsync(userId, sessionId),
            Times.Once
        );
    }

    [Fact]
    public async Task LogoutAsync_WithRefreshToken_RevokesToken()
    {
        // Arrange
        var refreshToken = "valid-refresh-token";

        // Act
        await _service.LogoutAsync(refreshToken);

        // Assert
        _mockRefreshToken.Verify(
            x => x.RevokeRefreshTokenAsync(refreshToken),
            Times.Once
        );
    }

    [Fact]
    public async Task LogoutAsync_WithNullOrEmptyToken_DoesNothing()
    {
        // Act
        await _service.LogoutAsync("");

        // Assert
        _mockRefreshToken.Verify(
            x => x.RevokeRefreshTokenAsync(It.IsAny<string>()),
            Times.Never
        );
    }
}
