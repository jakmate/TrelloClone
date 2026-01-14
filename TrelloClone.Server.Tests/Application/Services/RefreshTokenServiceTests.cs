using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

using Moq;

using TrelloClone.Server.Application.Interfaces;
using TrelloClone.Server.Application.Services;

using Xunit;

namespace TrelloClone.Server.Tests.Application;

public class RefreshTokenServiceTests
{
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly Mock<ILogger<RefreshTokenService>> _mockLogger;
    private readonly RefreshTokenService _service;

    public RefreshTokenServiceTests()
    {
        _mockCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<RefreshTokenService>>();
        _service = new RefreshTokenService(_mockCache.Object);
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_StoresTokenInCache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(byte[]));

        // Act
        var token = await _service.GenerateRefreshTokenAsync(userId);

        // Assert
        Assert.NotNull(token);
        _mockCache.Verify(x => x.SetAsync(
            It.Is<string>(k => k.StartsWith("refresh_token:")),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_AddsToUserTokenList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(byte[]));

        // Act
        await _service.GenerateRefreshTokenAsync(userId);

        // Assert
        _mockCache.Verify(x => x.SetAsync(
            $"user_refresh_tokens:{userId}",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_ValidToken_ReturnsTokenInfo()
    {
        // Arrange
        var tokenInfo = new RefreshTokenInfo
        {
            UserId = Guid.NewGuid(),
            DeviceId = "device1",
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        var tokenJson = JsonSerializer.Serialize(tokenInfo);
        _mockCache.Setup(x => x.GetAsync("refresh_token:validtoken", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(tokenJson));

        // Act
        var result = await _service.ValidateRefreshTokenAsync("validtoken");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tokenInfo.UserId, result.UserId);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_TokenNotFound_ReturnsNull()
    {
        // Arrange
        _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(byte[]));

        // Act
        var result = await _service.ValidateRefreshTokenAsync("invalidtoken");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_ExpiredToken_RevokesAndReturnsNull()
    {
        // Arrange
        var tokenInfo = new RefreshTokenInfo
        {
            UserId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
        var tokenJson = JsonSerializer.Serialize(tokenInfo);
        _mockCache.Setup(x => x.GetAsync("refresh_token:expiredtoken", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(tokenJson));

        // Act
        var result = await _service.ValidateRefreshTokenAsync("expiredtoken");

        // Assert
        Assert.Null(result);
        _mockCache.Verify(x => x.RemoveAsync("refresh_token:expiredtoken", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_RemovesTokenFromCache()
    {
        // Arrange
        var token = "token123";

        // Act
        await _service.RevokeRefreshTokenAsync(token);

        // Assert
        _mockCache.Verify(x => x.RemoveAsync($"refresh_token:{token}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_RevokesAllTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tokens = new List<string> { "token1", "token2", "token3" };
        var tokensJson = JsonSerializer.Serialize(tokens);
        _mockCache.Setup(x => x.GetAsync($"user_refresh_tokens:{userId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(tokensJson));

        // Act
        await _service.RevokeAllUserTokensAsync(userId);

        // Assert
        _mockCache.Verify(x => x.RemoveAsync(It.Is<string>(k => k.StartsWith("refresh_token:")), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
        _mockCache.Verify(x => x.RemoveAsync($"user_refresh_tokens:{userId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_WithException_KeepsExceptionToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tokens = new List<string> { "token1", "token2", "token3" };
        var tokensJson = JsonSerializer.Serialize(tokens);
        _mockCache.Setup(x => x.GetAsync($"user_refresh_tokens:{userId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(tokensJson));

        // Act
        await _service.RevokeAllUserTokensAsync(userId, "token2");

        // Assert
        _mockCache.Verify(x => x.RemoveAsync("refresh_token:token1", It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(x => x.RemoveAsync("refresh_token:token2", It.IsAny<CancellationToken>()), Times.Never);
        _mockCache.Verify(x => x.RemoveAsync("refresh_token:token3", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetActiveSessionAsync_StoresSessionInCache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = "session123";

        // Act
        await _service.SetActiveSessionAsync(userId, sessionId);

        // Assert
        _mockCache.Verify(x => x.SetAsync(
            $"active_session:{userId}",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetActiveSessionAsync_ReturnsSessionId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = "session123";
        _mockCache.Setup(x => x.GetAsync($"active_session:{userId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(sessionId));

        // Act
        var result = await _service.GetActiveSessionAsync(userId);

        // Assert
        Assert.Equal(sessionId, result);
    }

    [Fact]
    public async Task GetActiveSessionAsync_NoSession_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockCache.Setup(x => x.GetAsync($"active_session:{userId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(byte[]));

        // Act
        var result = await _service.GetActiveSessionAsync(userId);

        // Assert
        Assert.Null(result);
    }
}
