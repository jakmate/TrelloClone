using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;

using Moq;
using Moq.Protected;

using TrelloClone.Client.Services;
using TrelloClone.Shared.DTOs.Invitation;
using TrelloClone.Shared.Enums;

using Xunit;

namespace TrelloClone.Client.Tests.Services;

public class InvitationServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<AuthenticationStateProvider> _mockAuthProvider;
    private readonly InvitationService _service;

    public InvitationServiceTests()
    {
        _mockHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHandler.Object) { BaseAddress = new Uri("http://localhost") };
        _mockAuthProvider = new Mock<AuthenticationStateProvider>();
        _service = new InvitationService(_httpClient, _mockAuthProvider.Object);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, object? content = null)
    {
        var response = new HttpResponseMessage(statusCode);
        if (content != null)
        {
            response.Content = JsonContent.Create(content);
        }
        else
        {
            response.Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json");
        }

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void SetupAuthUser(string userId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "test");
        var user = new ClaimsPrincipal(identity);
        var authState = Task.FromResult(new AuthenticationState(user));
        _mockAuthProvider.Setup(x => x.GetAuthenticationStateAsync()).Returns(authState);
    }

    [Fact]
    public async Task GetPendingInvitations_ValidUser_ReturnsInvitations()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthUser(userId.ToString());
        var invitations = new List<BoardInvitationDto> { new BoardInvitationDto { Id = Guid.NewGuid() } };
        SetupHttpResponse(HttpStatusCode.OK, invitations);

        // Act
        var result = await _service.GetPendingInvitations();

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetPendingInvitations_NoUser_ReturnsEmptyList()
    {
        // Arrange
        SetupAuthUser("");

        // Act
        var result = await _service.GetPendingInvitations();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPendingInvitations_NullResponse_ReturnsEmptyList()
    {
        // Arrange
        SetupAuthUser(Guid.NewGuid().ToString());
        SetupHttpResponse(HttpStatusCode.OK, null);

        // Act
        var result = await _service.GetPendingInvitations();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task AcceptInvitation_SuccessfulResponse_Completes()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK);

        // Act
        await _service.AcceptInvitation(Guid.NewGuid());

        // Assert
        _mockHandler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task AcceptInvitation_FailedResponse_ThrowsHttpRequestException()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Error message")
        };
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => _service.AcceptInvitation(Guid.NewGuid()));
        Assert.Contains("Failed to accept invitation", ex.Message);
    }

    [Fact]
    public async Task DeclineInvitation_SuccessfulResponse_Completes()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK);

        // Act
        await _service.DeclineInvitation(Guid.NewGuid());

        // Assert
        _mockHandler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task DeclineInvitation_FailedResponse_ThrowsHttpRequestException()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Error message")
        };
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => _service.DeclineInvitation(Guid.NewGuid()));
        Assert.Contains("Failed to decline invitation", ex.Message);
    }

    [Fact]
    public async Task SendInvitation_SuccessfulResponse_Completes()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK);

        // Act
        await _service.SendInvitation(Guid.NewGuid(), "testuser", PermissionLevel.Editor);

        // Assert
        _mockHandler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendInvitation_FailedResponse_ThrowsHttpRequestException()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Error message")
        };
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            _service.SendInvitation(Guid.NewGuid(), "testuser"));
        Assert.Contains("Failed to send invitation", ex.Message);
    }

    [Fact]
    public async Task SendInvitation_DefaultPermission_UsesEditor()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK);
        HttpRequestMessage? capturedRequest = null;
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        await _service.SendInvitation(Guid.NewGuid(), "testuser");

        // Assert
        Assert.NotNull(capturedRequest);
        var content = await capturedRequest.Content!.ReadAsStringAsync();
        Assert.Contains("\"permission\":1", content);
    }
}
