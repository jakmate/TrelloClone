using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;

using TrelloClone.Server.Domain.Entities;
using TrelloClone.Server.Infrastructure.Persistence;
using TrelloClone.Shared.DTOs.Board;
using TrelloClone.Shared.Enums;

using Xunit;

namespace TrelloClone.Server.Tests.Infrastructure.Persistence;

public sealed class BoardRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly BoardRepository _repository;
    private bool _disposed; // Track disposal state

    public BoardRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new BoardRepository(_context);
    }

    [Fact]
    public async Task ExistsAsync_BoardExists_ReturnsTrue()
    {
        // Arrange
        var board = new Board { Id = Guid.NewGuid(), Name = "TestBoard" };
        _context.Boards.Add(board);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.ExistsAsync(board.Id);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_BoardDoesNotExist_ReturnsFalse()
    {
        // Act
        var exists = await _repository.ExistsAsync(Guid.NewGuid());

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task NameExistsAsync_NameExists_ReturnsTrue()
    {
        // Arrange
        var board = new Board { Name = "TestBoard" };
        var user = new User
        {
            Email = "TestEmail",
            PasswordHash = "TestHash",
            UserName = "TestUsername",
        };
        var boardUser = new BoardUser
        {
            BoardId = board.Id,
            Board = board,
            UserId = user.Id,
            User = user,
            PermissionLevel = PermissionLevel.Viewer,
        };
        _context.Boards.Add(board);
        _context.BoardUsers.Add(boardUser);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.NameExistsAsync("TestBoard", user.Id);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task NameExistsAsync_NameDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Email = "TestEmail",
            PasswordHash = "TestHash",
            UserName = "TestUsername",
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.NameExistsAsync("TestBoard", user.Id);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesColumnsAndTasks()
    {
        // Arrange
        var boardId = Guid.NewGuid();
        var columnId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _context.Boards.Add(new Board { Id = boardId, Name = "TestBoard" });
        _context.Columns.Add(new Column { Id = columnId, BoardId = boardId, Title = "TestColumn" });
        _context.Tasks.Add(new TaskItem
        {
            Id = taskId,
            ColumnId = columnId,
            Name = "TestTask"
        });
        await _context.SaveChangesAsync();

        // Act
        var board = await _repository.GetByIdAsync(boardId);

        // Assert
        Assert.NotNull(board);
        Assert.Single(board.Columns);
        Assert.Single(board.Columns.First().Tasks);
        Assert.Equal("TestTask", board.Columns.First().Tasks.First().Name);
    }

    [Fact]
    public async Task GetByIdAsync_BoardNotFound_ReturnsNull()
    {
        // Act
        var board = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(board);
    }

    [Fact]
    public async Task GetByIdWithMembersAsync_IncludesBoardUsers()
    {
        // Arrange
        var boardId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _context.Users.Add(new User { Id = userId, Email = "test@test.com", UserName = "test", PasswordHash = "testHash" });
        _context.Boards.Add(new Board { Id = boardId, Name = "TestBoard" });
        _context.BoardUsers.Add(new BoardUser { BoardId = boardId, UserId = userId });
        await _context.SaveChangesAsync();

        // Act
        var board = await _repository.GetByIdWithMembersAsync(boardId);

        // Assert
        Assert.NotNull(board);
        Assert.Single(board.BoardUsers);
        Assert.Equal(userId, board.BoardUsers.First().User.Id);
    }

    [Fact]
    public async Task GetAllByUserIdAsync_ReturnsUserBoardsOrderedByPosition()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var board1 = new Board { Id = Guid.NewGuid(), Name = "Board1", Position = 2 };
        var board2 = new Board { Id = Guid.NewGuid(), Name = "Board2", Position = 1 };

        _context.Users.Add(new User { Id = userId, Email = "test@test.com", UserName = "test", PasswordHash = "testHash" });
        _context.Boards.AddRange(board1, board2);
        _context.BoardUsers.AddRange(
            new BoardUser { BoardId = board1.Id, UserId = userId },
            new BoardUser { BoardId = board2.Id, UserId = userId }
        );
        await _context.SaveChangesAsync();

        // Act
        var boards = await _repository.GetAllByUserIdAsync(userId);

        // Assert
        Assert.Equal(2, boards.Count);
        Assert.Equal("Board2", boards[0].Name); // Position 1 comes first
        Assert.Equal("Board1", boards[1].Name); // Position 2 comes second
    }

    [Fact]
    public async Task UpdatePositionsAsync_UpdatesBoardPositions()
    {
        // Arrange
        var board1 = new Board { Id = Guid.NewGuid(), Name = "Board1", Position = 1 };
        var board2 = new Board { Id = Guid.NewGuid(), Name = "Board2", Position = 2 };
        _context.Boards.AddRange(board1, board2);
        await _context.SaveChangesAsync();

        var positions = new List<BoardPositionDto>
        {
            new BoardPositionDto { Id = board1.Id, Position = 3 },
            new BoardPositionDto { Id = board2.Id, Position = 1 }
        };

        // Act
        await _repository.UpdatePositionsAsync(positions);
        await _context.SaveChangesAsync();

        // Assert
        var updatedBoard1 = await _context.Boards.FindAsync(board1.Id);
        var updatedBoard2 = await _context.Boards.FindAsync(board2.Id);
        Assert.Equal(3, updatedBoard1!.Position);
        Assert.Equal(1, updatedBoard2!.Position);
    }

    [Fact]
    public async Task Add_AddsBoardToDatabase()
    {
        // Arrange
        var board = new Board
        {
            Id = Guid.NewGuid(),
            Name = "TestBoard"
        };

        // Act
        _repository.Add(board);
        await _context.SaveChangesAsync();

        // Assert
        var savedBoard = await _context.Boards.FindAsync(board.Id);
        Assert.NotNull(savedBoard);
        Assert.Equal(board.Id, savedBoard.Id);
    }

    [Fact]
    public async Task Update_UpdatesBoardInDatabase()
    {
        // Arrange
        var board = new Board { Name = "OldName" };
        _context.Boards.Add(board);
        await _context.SaveChangesAsync();

        // Act
        board.Name = "UpdatedName";
        _repository.Update(board);
        await _context.SaveChangesAsync();

        // Assert
        var updatedBoard = await _context.Boards.FindAsync(board.Id);
        Assert.NotNull(updatedBoard);
        Assert.Equal("UpdatedName", updatedBoard.Name);
    }

    [Fact]
    public async Task Remove_RemovesBoardFromDatabase()
    {
        // Arrange
        var board = new Board
        {
            Id = Guid.NewGuid(),
            Name = "TestBoard"
        };
        _context.Boards.Add(board);
        await _context.SaveChangesAsync();
        var boardId = board.Id;

        // Act
        _repository.Remove(board);
        await _context.SaveChangesAsync();

        // Assert
        var deletedBoard = await _context.Boards.FindAsync(boardId);
        Assert.Null(deletedBoard);
    }

    // IDisposable implementation for sealed class - stupid sonarqube
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                _context?.Dispose();
            }

            _disposed = true;
        }
    }
}
