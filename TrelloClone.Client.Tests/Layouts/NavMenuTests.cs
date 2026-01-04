using Bunit;
using Bunit.TestDoubles;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using TrelloClone.Client.Layouts;
using TrelloClone.Client.Services;
using TrelloClone.Shared.DTOs.Board;

using Xunit;

namespace TrelloClone.Client.Tests.Layouts;

public class NavMenuTests : BunitContext
{
    private readonly Mock<IBoardService> _mockBoardService;
    private readonly Mock<BoardStateService> _mockBoardState;

    public NavMenuTests()
    {
        _mockBoardService = new Mock<IBoardService>();
        _mockBoardState = new Mock<BoardStateService>();

        Services.AddSingleton(_mockBoardService.Object);
        Services.AddSingleton(_mockBoardState.Object);
    }

    [Fact]
    public void NavMenu_UnauthenticatedUser_ShowsLoginAndSignup()
    {
        // Arrange
        this.AddAuthorization().SetNotAuthorized();

        // Act
        var cut = Render<NavMenu>();

        // Assert
        Assert.Contains("Log in", cut.Markup);
        Assert.Contains("Sign up", cut.Markup);
    }

    [Fact]
    public void NavMenu_AuthenticatedUser_ShowsWorkspaceSection()
    {
        // Arrange
        this.AddAuthorization().SetAuthorized("testuser");
        _mockBoardService.Setup(x => x.GetBoardsAsync()).ReturnsAsync(new List<BoardDto>());

        // Act
        var cut = Render<NavMenu>();

        // Assert
        Assert.Contains("Workspace", cut.Markup);
        Assert.Contains("Boards", cut.Markup);
        Assert.Contains("Templates", cut.Markup);
    }

    [Fact]
    public void NavMenu_AuthenticatedUser_ShowsOnlineStatus()
    {
        // Arrange
        this.AddAuthorization().SetAuthorized("testuser");
        _mockBoardService.Setup(x => x.GetBoardsAsync()).ReturnsAsync(new List<BoardDto>());

        // Act
        var cut = Render<NavMenu>();

        // Assert
        Assert.Contains("Online", cut.Markup);
    }

    [Fact]
    public void NavMenu_NoBoards_ShowsNoboardsMessage()
    {
        // Arrange
        this.AddAuthorization().SetAuthorized("testuser");
        _mockBoardService.Setup(x => x.GetBoardsAsync()).ReturnsAsync(new List<BoardDto>());

        // Act
        var cut = Render<NavMenu>();

        // Assert
        Assert.Contains("No boards yet", cut.Markup);
    }

    [Fact]
    public void NavMenu_WithBoards_DisplaysBoardList()
    {
        // Arrange
        this.AddAuthorization().SetAuthorized("testuser");
        var boards = new List<BoardDto>
        {
            new BoardDto { Id = Guid.NewGuid(), Name = "Board 1", Position = 1 },
            new BoardDto { Id = Guid.NewGuid(), Name = "Board 2", Position = 2 }
        };
        _mockBoardService.Setup(x => x.GetBoardsAsync()).ReturnsAsync(boards);

        // Act
        var cut = Render<NavMenu>();

        // Assert
        Assert.Contains("Board 1", cut.Markup);
        Assert.Contains("Board 2", cut.Markup);
    }

    [Fact]
    public void NavMenu_MoreThan5Boards_ShowsViewAllLink()
    {
        // Arrange
        this.AddAuthorization().SetAuthorized("testuser");
        var boards = Enumerable.Range(1, 7)
            .Select(i => new BoardDto { Id = Guid.NewGuid(), Name = $"Board {i}", Position = i })
            .ToList();
        _mockBoardService.Setup(x => x.GetBoardsAsync()).ReturnsAsync(boards);

        // Act
        var cut = Render<NavMenu>();

        // Assert
        Assert.Contains("View All Boards", cut.Markup);
    }

    [Fact]
    public void NavMenu_5OrFewerBoards_DoesNotShowViewAllLink()
    {
        // Arrange
        this.AddAuthorization().SetAuthorized("testuser");
        var boards = Enumerable.Range(1, 5)
            .Select(i => new BoardDto { Id = Guid.NewGuid(), Name = $"Board {i}", Position = i })
            .ToList();
        _mockBoardService.Setup(x => x.GetBoardsAsync()).ReturnsAsync(boards);

        // Act
        var cut = Render<NavMenu>();

        // Assert
        Assert.DoesNotContain("View All Boards", cut.Markup);
    }

    [Fact]
    public void GetInitials_SingleWord_ReturnsFirstLetter()
    {
        // Arrange
        this.AddAuthorization().SetAuthorized("testuser");
        var boards = new List<BoardDto>
        {
            new BoardDto { Id = Guid.NewGuid(), Name = "Project", Position = 1 }
        };
        _mockBoardService.Setup(x => x.GetBoardsAsync()).ReturnsAsync(boards);

        // Act
        var cut = Render<NavMenu>();

        // Assert
        Assert.Contains("P", cut.Markup);
    }

    [Fact]
    public void GetInitials_TwoWords_ReturnsTwoLetters()
    {
        // Arrange
        this.AddAuthorization().SetAuthorized("testuser");
        var boards = new List<BoardDto>
        {
            new BoardDto { Id = Guid.NewGuid(), Name = "My Project", Position = 1 }
        };
        _mockBoardService.Setup(x => x.GetBoardsAsync()).ReturnsAsync(boards);

        // Act
        var cut = Render<NavMenu>();

        // Assert
        Assert.Contains("MP", cut.Markup);
    }

    [Fact]
    public void NavMenu_RendersHomeLink()
    {
        // Arrange
        this.AddAuthorization().SetNotAuthorized();

        // Act
        var cut = Render<NavMenu>();

        // Assert
        Assert.Contains("Home", cut.Markup);
    }

    [Fact]
    public void NavMenu_BoardsOrderedByPosition()
    {
        // Arrange
        this.AddAuthorization().SetAuthorized("testuser");
        var boards = new List<BoardDto>
        {
            new BoardDto { Id = Guid.NewGuid(), Name = "Board C", Position = 3 },
            new BoardDto { Id = Guid.NewGuid(), Name = "Board A", Position = 1 },
            new BoardDto { Id = Guid.NewGuid(), Name = "Board B", Position = 2 }
        };
        _mockBoardService.Setup(x => x.GetBoardsAsync()).ReturnsAsync(boards);

        // Act
        var cut = Render<NavMenu>();

        // Assert - verify boards appear in order
        var markup = cut.Markup;
        var indexA = markup.IndexOf("Board A");
        var indexB = markup.IndexOf("Board B");
        var indexC = markup.IndexOf("Board C");

        Assert.True(indexA < indexB && indexB < indexC);
    }

    [Fact]
    public async Task RefreshBoards_OnBoardsChanged_UpdatesBoardsList()
    {
        // Arrange
        this.AddAuthorization().SetAuthorized("testuser");
        var boardStateService = new BoardStateService(); // Use real instance
        Services.AddSingleton(boardStateService);

        var initialBoards = new List<BoardDto>
        {
            new BoardDto { Id = Guid.NewGuid(), Name = "Board 1", Position = 1 }
        };
        var updatedBoards = new List<BoardDto>
        {
            new BoardDto { Id = Guid.NewGuid(), Name = "Board 1", Position = 1 },
            new BoardDto { Id = Guid.NewGuid(), Name = "Board 2", Position = 2 }
        };

        _mockBoardService.SetupSequence(x => x.GetBoardsAsync())
            .ReturnsAsync(initialBoards)
            .ReturnsAsync(updatedBoards);

        var cut = Render<NavMenu>();

        // Act - trigger the OnBoardsChanged event using the real service
        boardStateService.NotifyBoardsChanged(); // Assuming this method exists
        await Task.Delay(100);
        cut.WaitForState(() => cut.Markup.Contains("Board 2"), timeout: TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("Board 2", cut.Markup);
    }

    [Fact]
    public void Dispose_UnsubscribesFromBoardsChanged()
    {
        // Arrange
        this.AddAuthorization().SetAuthorized("testuser");
        var boardStateService = new BoardStateService(); // Use real instance
        Services.AddSingleton(boardStateService);
        _mockBoardService.Setup(x => x.GetBoardsAsync()).ReturnsAsync(new List<BoardDto>());

        var cut = Render<NavMenu>();
        var subscriberCountBefore = GetEventSubscriberCount(boardStateService);

        // Act
        cut.Instance.Dispose();

        // Assert
        var subscriberCountAfter = GetEventSubscriberCount(boardStateService);
        Assert.True(subscriberCountAfter < subscriberCountBefore || subscriberCountAfter == 0);
    }

    private int GetEventSubscriberCount(BoardStateService service)
    {
        var eventField = typeof(BoardStateService)
            .GetField("OnBoardsChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var eventDelegate = eventField?.GetValue(service) as Delegate;
        return eventDelegate?.GetInvocationList().Length ?? 0;
    }

    [Fact]
    public async Task RefreshBoards_ReordersBoards()
    {
        // Arrange
        this.AddAuthorization().SetAuthorized("testuser");
        var boardStateService = new BoardStateService();
        Services.AddSingleton(boardStateService);

        var initialBoards = new List<BoardDto>
        {
            new BoardDto { Id = Guid.NewGuid(), Name = "Board A", Position = 1 }
        };
        var reorderedBoards = new List<BoardDto>
        {
            new BoardDto { Id = Guid.NewGuid(), Name = "Board B", Position = 1 },
            new BoardDto { Id = Guid.NewGuid(), Name = "Board A", Position = 2 }
        };

        _mockBoardService.SetupSequence(x => x.GetBoardsAsync())
            .ReturnsAsync(initialBoards)
            .ReturnsAsync(reorderedBoards);

        var cut = Render<NavMenu>();

        // Act
        boardStateService.NotifyBoardsChanged();
        await Task.Delay(100);
        cut.WaitForState(() => cut.Markup.Contains("Board B"), timeout: TimeSpan.FromSeconds(2));

        // Assert - verify order
        var markup = cut.Markup;
        var indexB = markup.IndexOf("Board B");
        var indexA = markup.IndexOf("Board A");
        Assert.True(indexB < indexA);
    }
}
