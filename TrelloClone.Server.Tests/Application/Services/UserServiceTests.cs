using Moq;

using TrelloClone.Server.Application.Services;
using TrelloClone.Server.Domain.Entities;
using TrelloClone.Server.Domain.Interfaces;
using TrelloClone.Shared.DTOs;

using Xunit;

namespace TrelloClone.Server.Tests.Application;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockUsers;
    private readonly Mock<IBoardRepository> _mockBoards;
    private readonly Mock<IBoardUserRepository> _mockBoardUsers;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _mockUsers = new Mock<IUserRepository>();
        _mockBoards = new Mock<IBoardRepository>();
        _mockBoardUsers = new Mock<IBoardUserRepository>();
        _mockUow = new Mock<IUnitOfWork>();
        _service = new UserService(_mockUsers.Object, _mockBoards.Object,
            _mockBoardUsers.Object, _mockUow.Object);
    }

    [Fact]
    public async Task AddUserToBoardAsync_BoardDoesNotExist_ThrowsKeyNotFoundException()
    {
        // Arrange
        var boardId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _mockBoards.Setup(x => x.ExistsAsync(boardId)).ReturnsAsync(false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.AddUserToBoardAsync(boardId, userId));
        Assert.Equal("Board not found.", ex.Message);
    }

    [Fact]
    public async Task AddUserToBoardAsync_UserDoesNotExist_ThrowsKeyNotFoundException()
    {
        // Arrange
        var boardId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _mockBoards.Setup(x => x.ExistsAsync(boardId)).ReturnsAsync(true);
        _mockUsers.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.AddUserToBoardAsync(boardId, userId));
        Assert.Equal("User not found.", ex.Message);
    }

    [Fact]
    public async Task AddUserToBoardAsync_UserAlreadyOnBoard_ThrowsInvalidOperationException()
    {
        // Arrange
        var boardId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _mockBoards.Setup(x => x.ExistsAsync(boardId)).ReturnsAsync(true);
        _mockUsers.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(true);
        _mockBoardUsers.Setup(x => x.ExistsAsync(boardId, userId)).ReturnsAsync(true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddUserToBoardAsync(boardId, userId));
        Assert.Equal("User already on board.", ex.Message);
    }

    [Fact]
    public async Task AddUserToBoardAsync_ValidRequest_AddsUserAndSaves()
    {
        // Arrange
        var boardId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _mockBoards.Setup(x => x.ExistsAsync(boardId)).ReturnsAsync(true);
        _mockUsers.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(true);
        _mockBoardUsers.Setup(x => x.ExistsAsync(boardId, userId)).ReturnsAsync(false);

        // Act
        await _service.AddUserToBoardAsync(boardId, userId);

        // Assert
        _mockBoardUsers.Verify(x => x.Add(It.Is<BoardUser>(bu =>
            bu.BoardId == boardId && bu.UserId == userId)), Times.Once);
        _mockUow.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetUserAsync_UserDoesNotExist_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUsers.Setup(x => x.GetByIdWithBoardsAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var result = await _service.GetUserAsync(userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserAsync_UserExists_ReturnsUserDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            BoardUsers = new List<BoardUser>
            {
                new BoardUser
                {
                    BoardId = boardId,
                    Board = new Board { Id = boardId, Name = "Test Board" }
                }
            }
        };
        _mockUsers.Setup(x => x.GetByIdWithBoardsAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _service.GetUserAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("testuser", result.UserName);
        Assert.Single(result.Boards);
        Assert.Equal(boardId, result.Boards[0].Id);
        Assert.Equal("Test Board", result.Boards[0].Name);
    }

    [Fact]
    public async Task GetUserAsync_UserWithMultipleBoards_ReturnsDtoWithAllBoards()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            BoardUsers = new List<BoardUser>
            {
                new BoardUser { Board = new Board { Id = Guid.NewGuid(), Name = "Board 1" } },
                new BoardUser { Board = new Board { Id = Guid.NewGuid(), Name = "Board 2" } }
            }
        };
        _mockUsers.Setup(x => x.GetByIdWithBoardsAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _service.GetUserAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Boards.Count);
    }
}
