using Moq;

using TrelloClone.Server.Application.Services;
using TrelloClone.Server.Domain.Entities;
using TrelloClone.Server.Domain.Interfaces;
using TrelloClone.Shared.DTOs.Board;
using TrelloClone.Shared.DTOs.Column;
using TrelloClone.Shared.DTOs.Task;
using TrelloClone.Shared.Enums;

using Xunit;

namespace TrelloClone.Server.Tests.Application.Services;

public class BoardServiceTests
{
    private readonly Mock<IBoardRepository> _mockBoards;
    private readonly Mock<IBoardUserRepository> _mockBoardUsers;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly BoardService _service;

    public BoardServiceTests()
    {
        _mockBoards = new Mock<IBoardRepository>();
        _mockBoardUsers = new Mock<IBoardUserRepository>();
        _mockUow = new Mock<IUnitOfWork>();
        _service = new BoardService(_mockBoards.Object, _mockBoardUsers.Object, _mockUow.Object);
    }

    [Fact]
    public async Task CreateBoardAsync_NameExists_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockBoards.Setup(x => x.NameExistsAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateBoardAsync("Test", Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateBoardAsync_FirstBoard_SetsPositionZero()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        _mockBoards.Setup(x => x.NameExistsAsync(It.IsAny<string>(), ownerId)).ReturnsAsync(false);
        _mockBoards.Setup(x => x.GetAllByUserIdAsync(ownerId)).ReturnsAsync(new List<Board>());

        // Act
        var result = await _service.CreateBoardAsync("Test", ownerId);

        // Assert
        Assert.Equal(0, result.Position);
        _mockBoards.Verify(x => x.Add(It.Is<Board>(b => b.Position == 0 && b.BoardUsers.Any(bu => bu.PermissionLevel == PermissionLevel.Owner))), Times.Once);
    }

    [Fact]
    public async Task CreateBoardAsync_ExistingBoards_SetsNextPosition()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        _mockBoards.Setup(x => x.NameExistsAsync(It.IsAny<string>(), ownerId)).ReturnsAsync(false);
        _mockBoards.Setup(x => x.GetAllByUserIdAsync(ownerId)).ReturnsAsync(new List<Board> { new Board { Position = 2 } });

        // Act
        var result = await _service.CreateBoardAsync("Test", ownerId);

        // Assert
        Assert.Equal(3, result.Position);
    }

    [Fact]
    public async Task UpdateBoardAsync_BoardNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockBoards.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Board?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateBoardAsync(Guid.NewGuid(), "New", Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateBoardAsync_NotMember_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _mockBoards.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Board());
        _mockBoardUsers.Setup(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.UpdateBoardAsync(Guid.NewGuid(), "New", Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateBoardAsync_DuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        var board = new Board { Name = "Old" };
        _mockBoards.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(board);
        _mockBoardUsers.Setup(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(true);
        _mockBoards.Setup(x => x.NameExistsAsync("New", It.IsAny<Guid>())).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateBoardAsync(Guid.NewGuid(), "New", Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateBoardAsync_SameName_SkipsNameCheck()
    {
        // Arrange
        var board = new Board { Name = "Test" };
        _mockBoards.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(board);
        _mockBoardUsers.Setup(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(true);

        // Act
        await _service.UpdateBoardAsync(Guid.NewGuid(), "Test", Guid.NewGuid());

        // Assert
        _mockBoards.Verify(x => x.NameExistsAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task UpdateBoardAsync_ValidRequest_UpdatesName()
    {
        // Arrange
        var board = new Board { Name = "Old" };
        _mockBoards.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(board);
        _mockBoardUsers.Setup(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(true);
        _mockBoards.Setup(x => x.NameExistsAsync("New", It.IsAny<Guid>())).ReturnsAsync(false);

        // Act
        var result = await _service.UpdateBoardAsync(Guid.NewGuid(), "New", Guid.NewGuid());

        // Assert
        Assert.Equal("New", board.Name);
        Assert.Equal("New", result.Name);
    }

    [Fact]
    public async Task DeleteBoardAsync_NotOwner_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _mockBoards.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Board());
        _mockBoardUsers.Setup(x => x.IsOwnerAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.DeleteBoardAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteBoardAsync_ValidRequest_RemovesBoard()
    {
        // Arrange
        var board = new Board();
        _mockBoards.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(board);
        _mockBoardUsers.Setup(x => x.IsOwnerAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(true);

        // Act
        await _service.DeleteBoardAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        _mockBoards.Verify(x => x.Remove(board), Times.Once);
    }

    [Fact]
    public async Task LeaveBoardAsync_IsOwner_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockBoardUsers.Setup(x => x.IsOwnerAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.LeaveBoardAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task LeaveBoardAsync_NotMember_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockBoardUsers.Setup(x => x.IsOwnerAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(false);
        _mockBoardUsers.Setup(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.LeaveBoardAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task LeaveBoardAsync_ValidRequest_RemovesUser()
    {
        // Arrange
        var boardId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _mockBoardUsers.Setup(x => x.IsOwnerAsync(boardId, userId)).ReturnsAsync(false);
        _mockBoardUsers.Setup(x => x.ExistsAsync(boardId, userId)).ReturnsAsync(true);

        // Act
        await _service.LeaveBoardAsync(boardId, userId);

        // Assert
        _mockBoardUsers.Verify(x => x.RemoveUserAsync(boardId, userId), Times.Once);
    }

    [Fact]
    public async Task GetBoardAsync_BoardNotFound_ReturnsNull()
    {
        // Arrange
        _mockBoards.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Board?)null);

        // Act
        var result = await _service.GetBoardAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllBoardsAsync_NoBoards_ReturnsNull()
    {
        // Arrange
        _mockBoards.Setup(x => x.GetAllByUserIdAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Board>());

        // Act
        var result = await _service.GetAllBoardsAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ReorderBoardsAsync_NoPermission_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var positions = new List<BoardPositionDto> { new BoardPositionDto { Id = Guid.NewGuid() } };
        _mockBoardUsers.Setup(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.ReorderBoardsAsync(positions, Guid.NewGuid()));
    }

    [Fact]
    public async Task ReorderBoardsAsync_ValidRequest_UpdatesPositions()
    {
        // Arrange
        var positions = new List<BoardPositionDto> { new BoardPositionDto { Id = Guid.NewGuid() } };
        _mockBoardUsers.Setup(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(true);

        // Act
        await _service.ReorderBoardsAsync(positions, Guid.NewGuid());

        // Assert
        _mockBoards.Verify(x => x.UpdatePositionsAsync(positions), Times.Once);
    }

    [Fact]
    public async Task CreateBoardFromTemplateAsync_CreatesWithColumns()
    {
        // Arrange
        var request = new CreateBoardFromTemplateRequest
        {
            Name = "Test",
            OwnerId = Guid.NewGuid(),
            Columns = new List<CreateColumnRequest>
            {
                new CreateColumnRequest { Title = "Col1", Position = 0, Tasks = new List<CreateTaskRequest>() }
            }
        };
        _mockBoards.Setup(x => x.NameExistsAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(false);
        _mockBoards.Setup(x => x.GetAllByUserIdAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Board>());

        // Act
        await _service.CreateBoardFromTemplateAsync(request);

        // Assert
        _mockBoards.Verify(x => x.Add(It.Is<Board>(b => b.Columns.Count == 1)), Times.Once);
    }

    [Fact]
    public async Task GetUserPermissionAsync_ReturnsPermissionLevel()
    {
        // Arrange
        var boardId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _mockBoardUsers.Setup(x => x.GetUserPermissionAsync(boardId, userId)).ReturnsAsync(PermissionLevel.Editor);

        // Act
        var result = await _service.GetUserPermissionAsync(boardId, userId);

        // Assert
        Assert.Equal(PermissionLevel.Editor, result);
    }

    [Fact]
    public async Task IsOwnerAsync_ReturnsOwnerStatus()
    {
        // Arrange
        var boardId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _mockBoardUsers.Setup(x => x.IsOwnerAsync(boardId, userId)).ReturnsAsync(true);

        // Act
        var result = await _service.IsOwnerAsync(boardId, userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetBoardAsync_ValidBoard_ReturnsMappedDto()
    {
        // Arrange
        var board = new Board { Id = Guid.NewGuid(), Name = "Test", Position = 1 };
        _mockBoards.Setup(x => x.GetByIdAsync(board.Id)).ReturnsAsync(board);

        // Act
        var result = await _service.GetBoardAsync(board.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(board.Id, result.Id);
        Assert.Equal(board.Name, result.Name);
        Assert.Equal(board.Position, result.Position);
    }

    [Fact]
    public async Task GetAllBoardsAsync_ReturnsArray()
    {
        // Arrange
        var boards = new List<Board>
        {
            new Board { Id = Guid.NewGuid(), Name = "B1", Position = 1 },
            new Board { Id = Guid.NewGuid(), Name = "B2", Position = 2 }
        };
        _mockBoards.Setup(x => x.GetAllByUserIdAsync(It.IsAny<Guid>())).ReturnsAsync(boards);

        // Act
        var result = await _service.GetAllBoardsAsync(Guid.NewGuid());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        Assert.Equal("B1", result[0].Name);
    }

    [Fact]
    public async Task CreateBoardFromTemplateAsync_IncrementsExistingBoardPositions()
    {
        // Arrange
        var existingBoards = new List<Board>
        {
            new Board { Position = 0 },
            new Board { Position = 1 }
        };
        var request = new CreateBoardFromTemplateRequest
        {
            Name = "Test",
            OwnerId = Guid.NewGuid(),
            Columns = new List<CreateColumnRequest>()
        };
        _mockBoards.Setup(x => x.NameExistsAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(false);
        _mockBoards.Setup(x => x.GetAllByUserIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingBoards);

        // Act
        await _service.CreateBoardFromTemplateAsync(request);

        // Assert
        Assert.Equal(1, existingBoards[0].Position);
        Assert.Equal(2, existingBoards[1].Position);
    }

    [Fact]
    public async Task CreateBoardFromTemplateAsync_CreatesTasksFromTemplate()
    {
        // Arrange
        var request = new CreateBoardFromTemplateRequest
        {
            Name = "Test",
            OwnerId = Guid.NewGuid(),
            Columns = new List<CreateColumnRequest>
            {
                new CreateColumnRequest
                {
                    Title = "Col1",
                    Position = 0,
                    Tasks = new List<CreateTaskRequest>
                    {
                        new CreateTaskRequest { Name = "Task1", Priority = PriorityLevel.High, Position = 0 }
                    }
                }
            }
        };
        _mockBoards.Setup(x => x.NameExistsAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(false);
        _mockBoards.Setup(x => x.GetAllByUserIdAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Board>());

        // Act
        await _service.CreateBoardFromTemplateAsync(request);

        // Assert
        _mockBoards.Verify(x => x.Add(It.Is<Board>(b =>
            b.Columns.ElementAt(0).Tasks.Count == 1 &&
            b.Columns.ElementAt(0).Tasks.ElementAt(0).Name == "Task1")),
            Times.Once);
    }

    [Fact]
    public async Task CreateBoardFromTemplateAsync_ThrowsException_WhenBoardNameExists()
    {
        // Arrange
        var request = new CreateBoardFromTemplateRequest
        {
            Name = "Existing Board Name",
            OwnerId = Guid.NewGuid(),
            Columns = new List<CreateColumnRequest>()
        };
        _mockBoards.Setup(x => x.NameExistsAsync(request.Name, request.OwnerId))
                .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateBoardFromTemplateAsync(request)
        );
    }
}
