using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;

using TrelloClone.Server.Domain.Entities;
using TrelloClone.Server.Infrastructure.Persistence;
using TrelloClone.Shared.Enums;

using Xunit;

namespace TrelloClone.Server.Tests.Infrastructure.Persistence;

public sealed class BoardUserRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly BoardUserRepository _repository;
    private bool _disposed; // Track disposal state

    public BoardUserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new BoardUserRepository(_context);
    }

    [Fact]
    public async Task ExistsAsync_BoardUserExists_ReturnsTrue()
    {
        // Arrange
        var boardUser = new BoardUser
        {
            BoardId = Guid.NewGuid(),
            Board = new Board { Name = "TestName" },
            UserId = Guid.NewGuid(),
            User = new User
            {
                Email = "TestEmail",
                PasswordHash = "TestHash",
                UserName = "TestUsername",
            },
            PermissionLevel = PermissionLevel.Viewer,
        };
        _context.BoardUsers.Add(boardUser);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.ExistsAsync(boardUser.BoardId, boardUser.UserId);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_BoardUserDoesNotExist_ReturnsFalse()
    {
        // Act
        var exists = await _repository.ExistsAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task GetUserPermissionAsync_PermissionExists_ReturnsPermission()
    {
        // Arrange
        var boardUser = new BoardUser
        {
            BoardId = Guid.NewGuid(),
            Board = new Board { Name = "TestName" },
            UserId = Guid.NewGuid(),
            User = new User
            {
                Email = "TestEmail",
                PasswordHash = "TestHash",
                UserName = "TestUsername",
            },
            PermissionLevel = PermissionLevel.Admin,
        };
        _context.BoardUsers.Add(boardUser);
        await _context.SaveChangesAsync();

        // Act
        var permission = await _repository.GetUserPermissionAsync(boardUser.BoardId, boardUser.UserId);

        // Assert
        Assert.Equal(PermissionLevel.Admin, permission);
    }

    [Fact]
    public async Task GetUserPermissionAsync_PermissionDoesNotExist_ReturnsViewer()
    {
        // Act
        var permission = await _repository.GetUserPermissionAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        Assert.Equal(PermissionLevel.Viewer, permission);
    }

    [Fact]
    public async Task IsOwnerAsync_IsOwner_ReturnsTrue()
    {
        // Arrange
        var boardUser = new BoardUser
        {
            BoardId = Guid.NewGuid(),
            Board = new Board { Name = "TestName" },
            UserId = Guid.NewGuid(),
            User = new User
            {
                Email = "TestEmail",
                PasswordHash = "TestHash",
                UserName = "TestUsername",
            },
            PermissionLevel = PermissionLevel.Owner,
        };
        _context.BoardUsers.Add(boardUser);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.IsOwnerAsync(boardUser.BoardId, boardUser.UserId);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task IsOwnerAsync_IsNotOwner_ReturnsFalse()
    {
        // Act
        var exists = await _repository.IsOwnerAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task Add_AddsUserToDatabase()
    {
        // Arrange
        var boardUser = new BoardUser
        {
            BoardId = Guid.NewGuid(),
            Board = new Board { Name = "TestName" },
            UserId = Guid.NewGuid(),
            User = new User
            {
                Email = "TestEmail",
                PasswordHash = "TestHash",
                UserName = "TestUsername",
            },
            PermissionLevel = PermissionLevel.Viewer,
        };

        // Act
        _repository.Add(boardUser);
        await _context.SaveChangesAsync();

        // Assert
        var savedBoardUser = await _context.BoardUsers.FindAsync(boardUser.BoardId, boardUser.UserId);
        Assert.NotNull(savedBoardUser);
        Assert.Equal(boardUser.BoardId, savedBoardUser.BoardId);
        Assert.Equal(boardUser.UserId, savedBoardUser.UserId);
    }

    [Fact]
    public async Task Remove_RemovesUserFromDatabase()
    {
        // Arrange
        var boardUser = new BoardUser
        {
            BoardId = Guid.NewGuid(),
            Board = new Board { Name = "TestName" },
            UserId = Guid.NewGuid(),
            User = new User
            {
                Email = "TestEmail",
                PasswordHash = "TestHash",
                UserName = "TestUsername",
            },
            PermissionLevel = PermissionLevel.Viewer,
        };
        _context.BoardUsers.Add(boardUser);
        await _context.SaveChangesAsync();
        var userId = boardUser.UserId;
        var boardId = boardUser.BoardId;

        // Act
        await _repository.RemoveUserAsync(boardUser.BoardId, boardUser.UserId);
        await _context.SaveChangesAsync();

        // Assert
        var deletedBoardUser = await _context.BoardUsers.FindAsync(boardId, userId);
        Assert.Null(deletedBoardUser);
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
