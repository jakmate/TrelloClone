using Moq;

using TrelloClone.Server.Application.Services;
using TrelloClone.Server.Domain.Entities;
using TrelloClone.Server.Domain.Interfaces;
using TrelloClone.Shared.DTOs.Task;
using TrelloClone.Shared.Enums;

using Xunit;

namespace TrelloClone.Server.Tests.Application;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _mockTasks;
    private readonly Mock<IColumnRepository> _mockColumns;
    private readonly Mock<IBoardRepository> _mockBoards;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly TaskService _service;

    public TaskServiceTests()
    {
        _mockTasks = new Mock<ITaskRepository>();
        _mockColumns = new Mock<IColumnRepository>();
        _mockBoards = new Mock<IBoardRepository>();
        _mockUow = new Mock<IUnitOfWork>();
        _service = new TaskService(_mockTasks.Object, _mockColumns.Object, _mockBoards.Object, _mockUow.Object);
    }

    [Fact]
    public async Task GetTasksForColumnAsync_ReturnsMappedTasks()
    {
        // Arrange
        var columnId = Guid.NewGuid();
        var tasks = new List<TaskItem>
        {
            new TaskItem { Id = Guid.NewGuid(), Name = "Task1", ColumnId = columnId, AssignedUsers = new List<User>() }
        };
        _mockTasks.Setup(x => x.ListByColumnAsync(columnId)).ReturnsAsync(tasks);

        // Act
        var result = await _service.GetTasksForColumnAsync(columnId);

        // Assert
        Assert.Single(result);
        Assert.Equal("Task1", result[0].Name);
    }

    [Fact]
    public async Task CreateTaskAsync_ColumnNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockColumns.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Column?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.CreateTaskAsync(new CreateTaskRequest { ColumnId = Guid.NewGuid() }));
    }

    [Fact]
    public async Task CreateTaskAsync_ValidRequest_CreatesTask()
    {
        // Arrange
        var columnId = Guid.NewGuid();
        var column = new Column { Id = columnId, BoardId = Guid.NewGuid() };
        _mockColumns.Setup(x => x.GetByIdAsync(columnId)).ReturnsAsync(column);

        var req = new CreateTaskRequest { Name = "New Task", ColumnId = columnId, Priority = PriorityLevel.High };

        // Act
        await _service.CreateTaskAsync(req);

        // Assert
        _mockTasks.Verify(x => x.Add(It.Is<TaskItem>(t => t.Name == "New Task")), Times.Once);
        _mockUow.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateTaskAsync_WithAssignedUsers_ValidatesBoardMembers()
    {
        // Arrange
        var boardId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var column = new Column { Id = Guid.NewGuid(), BoardId = boardId };
        var board = new Board { Id = boardId, BoardUsers = new List<BoardUser>() };

        _mockColumns.Setup(x => x.GetByIdAsync(column.Id)).ReturnsAsync(column);
        _mockBoards.Setup(x => x.GetByIdWithMembersAsync(boardId)).ReturnsAsync(board);

        var req = new CreateTaskRequest { ColumnId = column.Id, AssignedUserIds = new List<Guid> { userId } };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateTaskAsync(req));
    }

    [Fact]
    public async Task CreateTaskAsync_WithAssignedUsers_AssignsUsers()
    {
        // Arrange
        var boardId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var column = new Column { Id = Guid.NewGuid(), BoardId = boardId };
        var board = new Board
        {
            Id = boardId,
            BoardUsers = new List<BoardUser>
            {
                new BoardUser { User = new User { Id = userId } }
            }
        };

        _mockColumns.Setup(x => x.GetByIdAsync(column.Id)).ReturnsAsync(column);
        _mockBoards.Setup(x => x.GetByIdWithMembersAsync(boardId)).ReturnsAsync(board);

        var req = new CreateTaskRequest
        {
            Name = "Task",
            ColumnId = column.Id,
            AssignedUserIds = new List<Guid> { userId }
        };

        // Act
        await _service.CreateTaskAsync(req);

        // Assert
        _mockTasks.Verify(x => x.AssignUsersToTaskAsync(It.IsAny<Guid>(), It.Is<List<Guid>>(ids => ids.Contains(userId))), Times.Once);
    }

    [Fact]
    public async Task UpdateTaskAsync_TaskNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockTasks.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TaskItem?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UpdateTaskAsync(Guid.NewGuid(), new UpdateTaskRequest()));
    }

    [Fact]
    public async Task UpdateTaskAsync_UpdatesName()
    {
        // Arrange
        var task = new TaskItem { Id = Guid.NewGuid(), Name = "Old", ColumnId = Guid.NewGuid() };
        var column = new Column { Id = task.ColumnId, BoardId = Guid.NewGuid() };

        _mockTasks.SetupSequence(x => x.GetByIdAsync(task.Id))
            .ReturnsAsync(task)
            .ReturnsAsync(new TaskItem { Id = task.Id, Name = "New", ColumnId = task.ColumnId, AssignedUsers = new List<User>() });
        _mockColumns.Setup(x => x.GetByIdAsync(task.ColumnId)).ReturnsAsync(column);

        // Act
        await _service.UpdateTaskAsync(task.Id, new UpdateTaskRequest { Name = "New" });

        // Assert
        Assert.Equal("New", task.Name);
        _mockUow.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateTaskAsync_WithAssignedUsers_ValidatesMembers()
    {
        // Arrange
        var task = new TaskItem { Id = Guid.NewGuid(), ColumnId = Guid.NewGuid() };
        var column = new Column { Id = task.ColumnId, BoardId = Guid.NewGuid() };
        var board = new Board { Id = column.BoardId, BoardUsers = new List<BoardUser>() };

        _mockTasks.Setup(x => x.GetByIdAsync(task.Id)).ReturnsAsync(task);
        _mockColumns.Setup(x => x.GetByIdAsync(task.ColumnId)).ReturnsAsync(column);
        _mockBoards.Setup(x => x.GetByIdWithMembersAsync(column.BoardId)).ReturnsAsync(board);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateTaskAsync(task.Id, new UpdateTaskRequest { AssignedUserIds = new List<Guid> { Guid.NewGuid() } }));
    }

    [Fact]
    public async Task DeleteTaskAsync_TaskNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockTasks.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TaskItem?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteTaskAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteTaskAsync_RemovesTask()
    {
        // Arrange
        var task = new TaskItem { Id = Guid.NewGuid() };
        _mockTasks.Setup(x => x.GetByIdAsync(task.Id)).ReturnsAsync(task);

        // Act
        await _service.DeleteTaskAsync(task.Id);

        // Assert
        _mockTasks.Verify(x => x.Remove(task), Times.Once);
        _mockUow.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ReorderTasksAsync_UpdatesPositions()
    {
        // Arrange
        var positions = new List<TaskPositionDto> { new TaskPositionDto { Id = Guid.NewGuid(), Position = 1 } };

        // Act
        await _service.ReorderTasksAsync(positions);

        // Assert
        _mockTasks.Verify(x => x.UpdatePositionsAsync(positions), Times.Once);
        _mockUow.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAvailableUsersForTaskAsync_ColumnNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockColumns.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Column?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetAvailableUsersForTaskAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetAvailableUsersForTaskAsync_ReturnsBoardMembers()
    {
        // Arrange
        var columnId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var column = new Column { Id = columnId, BoardId = boardId };
        var board = new Board
        {
            Id = boardId,
            BoardUsers = new List<BoardUser>
            {
                new BoardUser { User = new User { Id = userId, Email = "test@test.com", UserName = "test" } }
            }
        };

        _mockColumns.Setup(x => x.GetByIdAsync(columnId)).ReturnsAsync(column);
        _mockBoards.Setup(x => x.GetByIdWithMembersAsync(boardId)).ReturnsAsync(board);

        // Act
        var result = await _service.GetAvailableUsersForTaskAsync(columnId);

        // Assert
        Assert.Single(result);
        Assert.Equal(userId, result[0].Id);
    }

    [Fact]
    public async Task UpdateTaskAsync_UpdatesPriority()
    {
        // Arrange
        var task = new TaskItem { Id = Guid.NewGuid(), Priority = PriorityLevel.Low, ColumnId = Guid.NewGuid() };
        var column = new Column { Id = task.ColumnId, BoardId = Guid.NewGuid() };

        _mockTasks.SetupSequence(x => x.GetByIdAsync(task.Id))
            .ReturnsAsync(task)
            .ReturnsAsync(new TaskItem { Id = task.Id, Priority = PriorityLevel.High, ColumnId = task.ColumnId, AssignedUsers = new List<User>() });
        _mockColumns.Setup(x => x.GetByIdAsync(task.ColumnId)).ReturnsAsync(column);

        // Act
        await _service.UpdateTaskAsync(task.Id, new UpdateTaskRequest { Priority = PriorityLevel.High });

        // Assert
        Assert.Equal(PriorityLevel.High, task.Priority);
    }

    [Fact]
    public async Task UpdateTaskAsync_ReloadsTaskWithAssignedUsers()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task = new TaskItem { Id = Guid.NewGuid(), ColumnId = Guid.NewGuid() };
        var updatedTask = new TaskItem
        {
            Id = task.Id,
            ColumnId = task.ColumnId,
            AssignedUsers = new List<User> { new User { Id = userId } }
        };

        _mockTasks.SetupSequence(x => x.GetByIdAsync(task.Id))
            .ReturnsAsync(task)
            .ReturnsAsync(updatedTask);
        _mockColumns.Setup(x => x.GetByIdAsync(task.ColumnId)).ReturnsAsync(new Column { Id = task.ColumnId });

        // Act
        var result = await _service.UpdateTaskAsync(task.Id, new UpdateTaskRequest { Name = "Updated" });

        // Assert
        Assert.Contains(userId, result.AssignedUserIds);
    }

    [Fact]
    public async Task GetTasksForColumnAsync_MapsAssignedUserIds()
    {
        // Arrange
        var columnId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tasks = new List<TaskItem>
        {
            new TaskItem
            {
                Id = Guid.NewGuid(),
                Name = "Task",
                ColumnId = columnId,
                AssignedUsers = new List<User> { new User { Id = userId } }
            }
        };
        _mockTasks.Setup(x => x.ListByColumnAsync(columnId)).ReturnsAsync(tasks);

        // Act
        var result = await _service.GetTasksForColumnAsync(columnId);

        // Assert
        Assert.Contains(userId, result[0].AssignedUserIds);
    }
}
