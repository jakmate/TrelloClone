using System.Collections.Concurrent;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using Moq;

using TrelloClone.Server.Application.Hubs;
using TrelloClone.Server.Application.Interfaces;
using TrelloClone.Shared.DTOs.Column;
using TrelloClone.Shared.DTOs.SignalR;
using TrelloClone.Shared.DTOs.Task;
using TrelloClone.Shared.Enums;

using Xunit;

namespace TrelloClone.Server.Tests.Application.Hubs;

public class BoardHubTests
{
    private readonly Mock<IBoardService> _mockBoardService;
    private readonly Mock<ILogger<BoardHub>> _mockLogger;
    private readonly Mock<IHubCallerClients> _mockClients;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly Mock<HubCallerContext> _mockContext;
    private readonly Mock<IGroupManager> _mockGroups;
    private readonly BoardHub _hub;

    public BoardHubTests()
    {
        _mockBoardService = new Mock<IBoardService>();
        _mockLogger = new Mock<ILogger<BoardHub>>();
        _mockClients = new Mock<IHubCallerClients>();
        _mockClientProxy = new Mock<IClientProxy>();
        _mockContext = new Mock<HubCallerContext>();
        _mockGroups = new Mock<IGroupManager>();

        var mockCaller = _mockClientProxy.As<ISingleClientProxy>();
        _mockClients.Setup(x => x.Caller).Returns(mockCaller.Object);
        _mockClients.Setup(x => x.OthersInGroup(It.IsAny<string>())).Returns(_mockClientProxy.Object);
        _mockClients.Setup(x => x.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);

        _hub = new BoardHub(_mockBoardService.Object, _mockLogger.Object)
        {
            Clients = _mockClients.Object,
            Context = _mockContext.Object,
            Groups = _mockGroups.Object
        };
    }

    [Fact]
    public async Task JoinBoard_InvalidBoardId_SendsError()
    {
        // Arrange
        _mockContext.Setup(x => x.UserIdentifier).Returns(Guid.NewGuid().ToString());

        // Act
        await _hub.JoinBoard("invalid");

        // Assert
        _mockClientProxy.Verify(x => x.SendCoreAsync("Error",
            It.Is<object[]>(o => o[0].ToString()!.Contains("Invalid")), default), Times.Once);
    }

    [Fact]
    public async Task JoinBoard_ValidRequest_AddsToGroup()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        _mockContext.Setup(x => x.UserIdentifier).Returns(userId.ToString());
        _mockContext.Setup(x => x.ConnectionId).Returns("conn1");
        _mockBoardService.Setup(x => x.GetUserPermissionAsync(boardId, userId))
            .ReturnsAsync(PermissionLevel.Editor);

        // Act
        await _hub.JoinBoard(boardId.ToString());

        // Assert
        _mockGroups.Verify(x => x.AddToGroupAsync("conn1", $"Board_{boardId}", default), Times.Once);
    }

    [Fact]
    public async Task LeaveBoard_RemovesFromGroup()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        _mockContext.Setup(x => x.UserIdentifier).Returns(userId.ToString());
        _mockContext.Setup(x => x.ConnectionId).Returns("conn1");

        // Act
        await _hub.LeaveBoard(boardId.ToString());

        // Assert
        _mockGroups.Verify(x => x.RemoveFromGroupAsync("conn1", $"Board_{boardId}", default), Times.Once);
    }

    [Fact]
    public async Task TaskDragStarted_NotifiesOthers()
    {
        // Arrange
        var boardId = Guid.NewGuid().ToString();
        var dragInfo = new TaskDragInfo { TaskId = Guid.NewGuid().ToString() };

        // Act
        await _hub.TaskDragStarted(boardId, dragInfo);

        // Assert
        _mockClientProxy.Verify(x => x.SendCoreAsync("TaskDragStarted",
            It.Is<object[]>(o => o[0] == dragInfo), default), Times.Once);
    }

    [Fact]
    public async Task TaskDragEnded_NotifiesOthers()
    {
        // Arrange
        var boardId = Guid.NewGuid().ToString();
        var taskId = Guid.NewGuid().ToString();

        // Act
        await _hub.TaskDragEnded(boardId, taskId);

        // Assert
        _mockClientProxy.Verify(x => x.SendCoreAsync("TaskDragEnded",
            It.Is<object[]>(o => (string)o[0] == taskId), default), Times.Once);
    }

    [Fact]
    public async Task TaskMoved_NotifiesOthers()
    {
        // Arrange
        var boardId = Guid.NewGuid().ToString();
        var dragInfo = new TaskMoveInfo { TaskId = Guid.NewGuid().ToString() };

        // Act
        await _hub.TaskMoved(boardId, dragInfo);

        // Assert
        _mockClientProxy.Verify(x => x.SendCoreAsync("TaskMoved",
            It.Is<object[]>(o => o[0] == dragInfo), default), Times.Once);
    }

    [Fact]
    public async Task ColumnDragStarted_NotifiesOthers()
    {
        // Arrange
        var boardId = Guid.NewGuid().ToString();
        var dragInfo = new ColumnDragInfo { ColumnId = Guid.NewGuid().ToString() };

        // Act
        await _hub.ColumnDragStarted(boardId, dragInfo);

        // Assert
        _mockClientProxy.Verify(x => x.SendCoreAsync("ColumnDragStarted",
            It.Is<object[]>(o => o[0] == dragInfo), default), Times.Once);
    }

    [Fact]
    public async Task ColumnDragEnded_NotifiesOthers()
    {
        // Arrange
        var boardId = Guid.NewGuid().ToString();
        var columnId = Guid.NewGuid().ToString();

        // Act
        await _hub.ColumnDragEnded(boardId, columnId);

        // Assert
        _mockClientProxy.Verify(x => x.SendCoreAsync("ColumnDragEnded",
            It.Is<object[]>(o => (string)o[0] == columnId), default), Times.Once);
    }

    [Fact]
    public async Task ColumnMoved_NotifiesOthers()
    {
        // Arrange
        var boardId = Guid.NewGuid().ToString();
        var dragInfo = new ColumnMoveInfo { ColumnId = Guid.NewGuid().ToString() };

        // Act
        await _hub.ColumnMoved(boardId, dragInfo);

        // Assert
        _mockClientProxy.Verify(x => x.SendCoreAsync("ColumnMoved",
            It.Is<object[]>(o => o[0] == dragInfo), default), Times.Once);
    }

    [Fact]
    public async Task TaskCreated_NotifiesOthers()
    {
        // Arrange
        var boardId = Guid.NewGuid().ToString();
        var task = new TaskDto { Id = Guid.NewGuid() };

        // Act
        await _hub.TaskCreated(boardId, task);

        // Assert
        _mockClientProxy.Verify(x => x.SendCoreAsync("TaskCreated",
            It.Is<object[]>(o => o[0] == task), default), Times.Once);
    }


    [Fact]
    public async Task TaskUpdated_NotifiesOthersInGroup()
    {
        // Arrange
        var boardId = Guid.NewGuid().ToString();
        var task = new TaskDto { Id = Guid.NewGuid() };

        // Act
        await _hub.TaskUpdated(boardId, task);

        // Assert
        _mockClientProxy.Verify(x => x.SendCoreAsync("TaskUpdated",
            It.Is<object[]>(o => o[0] == task), default), Times.Once);
    }

    [Fact]
    public async Task TaskDeleted_NotifiesOthersInGroup()
    {
        // Arrange
        var boardId = Guid.NewGuid().ToString();
        var taskId = Guid.NewGuid().ToString();
        var columnId = Guid.NewGuid().ToString();

        // Act
        await _hub.TaskDeleted(boardId, taskId, columnId);

        // Assert
        _mockClientProxy.Verify(x => x.SendCoreAsync("TaskDeleted",
            It.Is<object[]>(o => ((TaskDeleteInfo)o[0]).TaskId == taskId && ((TaskDeleteInfo)o[0]).ColumnId == columnId),
                default), Times.Once);
    }

    [Fact]
    public async Task ColumnCreated_NotifiesOthers()
    {
        // Arrange
        var boardId = Guid.NewGuid().ToString();
        var column = new ColumnDto { Id = Guid.NewGuid() };

        // Act
        await _hub.ColumnCreated(boardId, column);

        // Assert
        _mockClientProxy.Verify(x => x.SendCoreAsync("ColumnCreated",
            It.Is<object[]>(o => o[0] == column), default), Times.Once);
    }

    [Fact]
    public async Task ColumnUpdated_NotifiesOthersInGroup()
    {
        // Arrange
        var boardId = Guid.NewGuid().ToString();
        var column = new ColumnDto { Id = Guid.NewGuid() };

        // Act
        await _hub.ColumnUpdated(boardId, column);

        // Assert
        _mockClientProxy.Verify(x => x.SendCoreAsync("ColumnUpdated",
            It.Is<object[]>(o => o[0] == column), default), Times.Once);
    }

    [Fact]
    public async Task ColumnDeleted_NotifiesOthersInGroup()
    {
        // Arrange
        var boardId = Guid.NewGuid().ToString();
        var columnId = Guid.NewGuid().ToString();

        // Act
        await _hub.ColumnDeleted(boardId, columnId);

        // Assert
        _mockClientProxy.Verify(x => x.SendCoreAsync("ColumnDeleted",
            It.Is<object[]>(o => o[0].ToString() == columnId), default), Times.Once);
    }

    [Fact]
    public async Task UserStartedEditing_NotifiesOthers()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _mockContext.Setup(x => x.UserIdentifier).Returns(userId);

        // Act
        await _hub.UserStartedEditing("board1", "task", "task1");

        // Assert
        _mockClientProxy.Verify(x => x.SendCoreAsync("UserStartedEditing",
            It.IsAny<object[]>(), default), Times.Once);
    }

    [Fact]
    public async Task UserStoppedEditing_NotifiesOthers()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var boardId = Guid.NewGuid().ToString();
        var itemType = "task";
        var itemId = Guid.NewGuid().ToString();

        _mockContext.Setup(x => x.UserIdentifier).Returns(userId);

        // Act
        await _hub.UserStoppedEditing(boardId, itemType, itemId);

        // Assert
        _mockClientProxy.Verify(x => x.SendCoreAsync("UserStoppedEditing",
            It.Is<object[]>(o =>
                ((UserStopEditInfo)o[0]).UserId == userId &&
                ((UserStopEditInfo)o[0]).ItemType == itemType &&
                ((UserStopEditInfo)o[0]).ItemId == itemId),
            default), Times.Once);
    }
}
