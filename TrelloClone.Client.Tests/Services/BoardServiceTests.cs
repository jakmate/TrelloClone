using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;

using Moq;
using Moq.Protected;

using TrelloClone.Client.Services;
using TrelloClone.Shared.DTOs.Board;
using TrelloClone.Shared.Enums;

using Xunit;

namespace TrelloClone.Client.Tests.Services;

public class BoardServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<AuthenticationStateProvider> _authStateProviderMock;

    public BoardServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost/")
        };
        _authStateProviderMock = new Mock<AuthenticationStateProvider>();
    }

    // Helper method to create a mock AuthenticationState
    private AuthenticationState CreateAuthState(Guid userId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);
        return new AuthenticationState(user);
    }

    [Fact]
    public async Task GetBoardsAsync_ReturnsBoards_WhenUserIsAuthenticated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var boards = new[] { new BoardDto { Id = Guid.NewGuid(), Name = "Test Board" } };
        var authState = CreateAuthState(userId);

        _authStateProviderMock
            .Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains($"api/boards?ownerId={userId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(boards)
            });

        var service = new BoardService(_httpClient, _authStateProviderMock.Object);

        // Act
        var result = await service.GetBoardsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Test Board", result[0].Name);
    }

    [Fact]
    public async Task GetBoardsAsync_ReturnsEmptyList_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var authState = new AuthenticationState(new ClaimsPrincipal());

        _authStateProviderMock
            .Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);

        var service = new BoardService(_httpClient, _authStateProviderMock.Object);

        // Act
        var result = await service.GetBoardsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBoardAsync_ReturnsBoard_WhenBoardExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var boardDto = new BoardDto { Id = id, Name = "Test Board" };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains($"api/boards/{id}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(boardDto)
            });

        var service = new BoardService(_httpClient, _authStateProviderMock.Object);

        // Act
        var result = await service.GetBoardAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Board", result.Name);
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task CreateBoardAsync_CreatesBoard_WhenUserIsAuthenticated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreateBoardRequest { Name = "New Board" };
        var boardDto = new BoardDto { Id = Guid.NewGuid(), Name = "New Board" };
        var authState = CreateAuthState(userId);

        _authStateProviderMock
            .Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("api/boards")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(boardDto)
            });

        var service = new BoardService(_httpClient, _authStateProviderMock.Object);

        // Act
        var result = await service.CreateBoardAsync(request);

        // Assert
        Assert.Equal("New Board", result.Name);
        Assert.Equal(userId, request.OwnerId);
    }

    [Fact]
    public async Task CreateBoardAsync_ThrowsUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var request = new CreateBoardRequest { Name = "New Board" };
        var authState = new AuthenticationState(new ClaimsPrincipal());

        _authStateProviderMock
            .Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);

        var service = new BoardService(_httpClient, _authStateProviderMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateBoardAsync(request));
    }

    [Fact]
    public async Task UpdateBoardAsync_UpdatesBoard_WhenRequestIsValid()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateBoardRequest { Name = "Updated Board" };
        var boardDto = new BoardDto { Id = id, Name = "Updated Board" };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri!.ToString().Contains($"api/boards/{id}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(boardDto)
            });

        var service = new BoardService(_httpClient, _authStateProviderMock.Object);

        // Act
        var result = await service.UpdateBoardAsync(id, request);

        // Assert
        Assert.Equal("Updated Board", result.Name);
    }

    [Fact]
    public async Task UpdateBoardAsync_ThrowsException_WhenRequestFails()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateBoardRequest { Name = "Updated Board" };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri!.ToString().Contains($"api/boards/{id}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest
            });

        var service = new BoardService(_httpClient, _authStateProviderMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => service.UpdateBoardAsync(id, request));
    }

    [Fact]
    public async Task DeleteBoardAsync_ReturnsTrue_WhenDeleteIsSuccessful()
    {
        // Arrange
        var id = Guid.NewGuid();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Delete &&
                    req.RequestUri!.ToString().Contains($"api/boards/{id}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent
            });

        var service = new BoardService(_httpClient, _authStateProviderMock.Object);

        // Act
        var result = await service.DeleteBoardAsync(id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteBoardAsync_ThrowsException_WhenDeleteFails()
    {
        // Arrange
        var id = Guid.NewGuid();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Delete &&
                    req.RequestUri!.ToString().Contains($"api/boards/{id}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Delete failed")
            });

        var service = new BoardService(_httpClient, _authStateProviderMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => service.DeleteBoardAsync(id));
        Assert.Contains("Delete failed", ex.Message);
    }

    [Fact]
    public async Task LeaveBoardAsync_ReturnsTrue_WhenLeaveIsSuccessful()
    {
        // Arrange
        var id = Guid.NewGuid();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains($"api/boards/{id}/leave")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        var service = new BoardService(_httpClient, _authStateProviderMock.Object);

        // Act
        var result = await service.LeaveBoardAsync(id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsOwnerAsync_ReturnsTrue_WhenUserIsOwner()
    {
        // Arrange
        var boardId = Guid.NewGuid();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains($"api/boards/{boardId}/permission")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(PermissionLevel.Owner)
            });

        var service = new BoardService(_httpClient, _authStateProviderMock.Object);

        // Act
        var result = await service.IsOwnerAsync(boardId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanEditAsync_ReturnsTrue_WhenUserIsEditor()
    {
        // Arrange
        var boardId = Guid.NewGuid();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains($"api/boards/{boardId}/permission")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(PermissionLevel.Editor)
            });

        var service = new BoardService(_httpClient, _authStateProviderMock.Object);

        // Act
        var result = await service.CanEditAsync(boardId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanInviteAsync_ReturnsTrue_WhenUserIsEditor()
    {
        // Arrange
        var boardId = Guid.NewGuid();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains($"api/boards/{boardId}/permission")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(PermissionLevel.Admin)
            });

        var service = new BoardService(_httpClient, _authStateProviderMock.Object);

        // Act
        var result = await service.CanInviteAsync(boardId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ReorderBoardsAsync_ReturnsTrue_WhenReorderIsSuccessful()
    {
        // Arrange
        var positions = new List<BoardPositionDto>
    {
        new BoardPositionDto { Id = Guid.NewGuid(), Position = 1 }
    };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri!.ToString().Contains("api/boards/reorder")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        var service = new BoardService(_httpClient, _authStateProviderMock.Object);

        // Act
        var result = await service.ReorderBoardsAsync(positions);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CreateBoardFromTemplateAsync_CreatesBoard_WhenUserIsAuthenticated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreateBoardFromTemplateRequest { };
        var boardDto = new BoardDto { Id = Guid.NewGuid(), Name = "From Template" };
        var authState = CreateAuthState(userId);

        _authStateProviderMock
            .Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("api/boards/from-template")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(boardDto)
            });

        var service = new BoardService(_httpClient, _authStateProviderMock.Object);

        // Act
        var result = await service.CreateBoardFromTemplateAsync(request);

        // Assert
        Assert.Equal("From Template", result.Name);
        Assert.Equal(userId, request.OwnerId);
    }
}
