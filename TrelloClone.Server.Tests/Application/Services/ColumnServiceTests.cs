using Moq;

using TrelloClone.Server.Application.Services;
using TrelloClone.Server.Domain.Entities;
using TrelloClone.Server.Domain.Interfaces;
using TrelloClone.Shared.DTOs.Column;

using Xunit;

namespace TrelloClone.Server.Tests.Application;

public class ColumnServiceTests
{
    private readonly Mock<IColumnRepository> _mockColumns;
    private readonly Mock<IBoardRepository> _mockBoards;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly ColumnService _service;

    public ColumnServiceTests()
    {
        _mockColumns = new Mock<IColumnRepository>();
        _mockBoards = new Mock<IBoardRepository>();
        _mockUow = new Mock<IUnitOfWork>();
        _service = new ColumnService(_mockColumns.Object, _mockBoards.Object, _mockUow.Object);
    }

    [Fact]
    public async Task GetColumnsForBoardAsync_ReturnsOrderedColumns()
    {
        // Arrange
        var columns = new List<Column>
        {
            new Column { Id = Guid.NewGuid(), Position = 2, Tasks = new List<TaskItem>() },
            new Column { Id = Guid.NewGuid(), Position = 1, Tasks = new List<TaskItem>() }
        };
        _mockColumns.Setup(x => x.ListByBoardAsync(It.IsAny<Guid>())).ReturnsAsync(columns);

        // Act
        var result = await _service.GetColumnsForBoardAsync(Guid.NewGuid());

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result[0].Position < result[1].Position);
    }

    [Fact]
    public async Task GetColumnsForBoardAsync_OrdersTasksByPosition()
    {
        // Arrange
        var columns = new List<Column>
        {
            new Column
            {
                Id = Guid.NewGuid(),
                Position = 1,
                Tasks = new List<TaskItem>
                {
                    new TaskItem { Position = 2, AssignedUsers = new List<User>() },
                    new TaskItem { Position = 1, AssignedUsers = new List<User>() }
                }
            }
        };
        _mockColumns.Setup(x => x.ListByBoardAsync(It.IsAny<Guid>())).ReturnsAsync(columns);

        // Act
        var result = await _service.GetColumnsForBoardAsync(Guid.NewGuid());

        // Assert
        Assert.True(result[0].Tasks[0].Position < result[0].Tasks[1].Position);
    }

    [Fact]
    public async Task GetColumnsForBoardAsync_MapsTaskAssignedUserIds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var columns = new List<Column>
        {
            new Column
            {
                Id = Guid.NewGuid(),
                Position = 1,
                Tasks = new List<TaskItem>
                {
                    new TaskItem
                    {
                        Position = 1,
                        AssignedUsers = new List<User> { new User { Id = userId } }
                    }
                }
            }
        };
        _mockColumns.Setup(x => x.ListByBoardAsync(It.IsAny<Guid>())).ReturnsAsync(columns);

        // Act
        var result = await _service.GetColumnsForBoardAsync(Guid.NewGuid());

        // Assert
        Assert.Contains(userId, result[0].Tasks[0].AssignedUserIds);
    }

    [Fact]
    public async Task CreateColumnAsync_BoardNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockBoards.Setup(x => x.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        // Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.CreateColumnAsync(new CreateColumnRequest { BoardId = Guid.NewGuid() }));
    }

    [Fact]
    public async Task CreateColumnAsync_TitleExists_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockBoards.Setup(x => x.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(true);
        _mockColumns.Setup(x => x.TitleExistsAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(true);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateColumnAsync(new CreateColumnRequest { BoardId = Guid.NewGuid(), Title = "Test" }));
    }

    [Fact]
    public async Task CreateColumnAsync_ValidRequest_SetsNextPosition()
    {
        // Arrange
        var boardId = Guid.NewGuid();
        _mockBoards.Setup(x => x.ExistsAsync(boardId)).ReturnsAsync(true);
        _mockColumns.Setup(x => x.TitleExistsAsync(boardId, It.IsAny<string>())).ReturnsAsync(false);
        _mockColumns.Setup(x => x.ListByBoardAsync(boardId))
            .ReturnsAsync(new List<Column> { new Column { Position = 2 } });

        // Act
        var result = await _service.CreateColumnAsync(new CreateColumnRequest { BoardId = boardId, Title = "Test" });

        // Assert
        Assert.Equal(3, result.Position);
        _mockColumns.Verify(x => x.Add(It.Is<Column>(c => c.Position == 3)), Times.Once);
    }

    [Fact]
    public async Task CreateColumnAsync_FirstColumn_SetsPositionZero()
    {
        // Arrange
        var boardId = Guid.NewGuid();
        _mockBoards.Setup(x => x.ExistsAsync(boardId)).ReturnsAsync(true);
        _mockColumns.Setup(x => x.TitleExistsAsync(boardId, It.IsAny<string>())).ReturnsAsync(false);
        _mockColumns.Setup(x => x.ListByBoardAsync(boardId)).ReturnsAsync(new List<Column>());

        // Act
        var result = await _service.CreateColumnAsync(new CreateColumnRequest { BoardId = boardId, Title = "Test" });

        // Assert
        Assert.Equal(0, result.Position);
    }

    [Fact]
    public async Task UpdateColumnAsync_EmptyTitle_ThrowsInvalidOperationException()
    {
        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateColumnAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateColumnRequest { Title = "" }));
    }

    [Fact]
    public async Task UpdateColumnAsync_ColumnNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockColumns.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Column?)null);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateColumnAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateColumnRequest { Title = "Test" }));
    }

    [Fact]
    public async Task UpdateColumnAsync_DuplicateTitle_ThrowsInvalidOperationException()
    {
        // Arrange
        var column = new Column { Title = "Old" };
        _mockColumns.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(column);
        _mockColumns.Setup(x => x.TitleExistsAsync(It.IsAny<Guid>(), "New")).ReturnsAsync(true);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateColumnAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateColumnRequest { Title = "New" }));
    }

    [Fact]
    public async Task UpdateColumnAsync_SameTitle_DoesNotCheckExists()
    {
        // Arrange
        var column = new Column { Title = "Test" };
        _mockColumns.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(column);

        // Act
        await _service.UpdateColumnAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateColumnRequest { Title = "Test" });

        // Assert
        _mockColumns.Verify(x => x.TitleExistsAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateColumnAsync_ValidRequest_UpdatesTitle()
    {
        // Arrange
        var column = new Column { Id = Guid.NewGuid(), Title = "Old" };
        _mockColumns.Setup(x => x.GetByIdAsync(column.Id)).ReturnsAsync(column);
        _mockColumns.Setup(x => x.TitleExistsAsync(It.IsAny<Guid>(), "New")).ReturnsAsync(false);

        // Act
        var result = await _service.UpdateColumnAsync(Guid.NewGuid(), column.Id, new UpdateColumnRequest { Title = "New" });

        // Assert
        Assert.Equal("New", column.Title);
        Assert.Equal("New", result.Title);
        _mockUow.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteColumnAsync_ColumnNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockColumns.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Column?)null);

        // Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteColumnAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteColumnAsync_ValidRequest_RemovesColumn()
    {
        // Arrange
        var column = new Column { Id = Guid.NewGuid() };
        _mockColumns.Setup(x => x.GetByIdAsync(column.Id)).ReturnsAsync(column);

        // Act
        await _service.DeleteColumnAsync(column.Id);

        // Assert
        _mockColumns.Verify(x => x.Remove(column), Times.Once);
        _mockUow.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ReorderColumnsAsync_UpdatesPositions()
    {
        // Arrange
        var positions = new List<ColumnPositionDto> { new ColumnPositionDto { Id = Guid.NewGuid(), Position = 1 } };

        // Act
        await _service.ReorderColumnsAsync(Guid.NewGuid(), positions);

        // Assert
        _mockColumns.Verify(x => x.UpdatePositionsAsync(positions), Times.Once);
        _mockUow.Verify(x => x.SaveChangesAsync(), Times.Once);
    }
}
