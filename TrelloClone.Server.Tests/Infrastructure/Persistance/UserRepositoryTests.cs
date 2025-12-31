using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;

using TrelloClone.Server.Domain.Entities;
using TrelloClone.Server.Infrastructure.Persistance;

using Xunit;

namespace TrelloClone.Server.Tests.Infrastructure.Persistance;

public sealed class UserRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UserRepository _repository;
    private readonly string _passwordHash = "dummy_hash_for_testing";
    private bool _disposed; // Track disposal state

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new UserRepository(_context);
    }

    [Fact]
    public async Task ExistsAsync_UserExists_ReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            UserName = "test",
            PasswordHash = _passwordHash
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.ExistsAsync(user.Id);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_UserDoesNotExist_ReturnsFalse()
    {
        // Act
        var exists = await _repository.ExistsAsync(Guid.NewGuid());

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task GetByIdAsync_UserExists_ReturnsUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@test.com",
            UserName = "test",
            PasswordHash = _passwordHash
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_UserDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdWithBoardsAsync_IncludesBoards()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var boardId = Guid.NewGuid();

        _context.Users.Add(new User
        {
            Id = userId,
            Email = "test@test.com",
            UserName = "test",
            PasswordHash = _passwordHash
        });

        _context.Boards.Add(new Board { Id = boardId, Name = "Test Board" });
        _context.BoardUsers.Add(new BoardUser { UserId = userId, BoardId = boardId });
        await _context.SaveChangesAsync();

        // Act
        var user = await _repository.GetByIdWithBoardsAsync(userId);

        // Assert
        Assert.NotNull(user);
        Assert.Single(user.BoardUsers);
        Assert.Equal(boardId, user.BoardUsers.First().Board.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_UserExists_ReturnsUser()
    {
        // Arrange
        var email = "test@test.com";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = "test",
            PasswordHash = _passwordHash
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEmailAsync(email);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_UserDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByEmailAsync("nonexistent@test.com");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUsernameAsync_UserExists_ReturnsUser()
    {
        // Arrange
        var username = "testuser";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            UserName = username,
            PasswordHash = _passwordHash
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUsernameAsync(username);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(username, result.UserName);
    }

    [Fact]
    public async Task GetByUsernameAsync_UserDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByUsernameAsync("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        // Arrange
        var user1 = new User
        {
            Id = Guid.NewGuid(),
            Email = "user1@test.com",
            UserName = "user1",
            PasswordHash = _passwordHash
        };
        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "user2@test.com",
            UserName = "user2",
            PasswordHash = _passwordHash
        };
        _context.Users.AddRange(user1, user2);
        await _context.SaveChangesAsync();

        // Act
        var users = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(2, users.Count);
        Assert.Contains(users, u => u.Email == "user1@test.com");
        Assert.Contains(users, u => u.Email == "user2@test.com");
    }

    [Fact]
    public async Task Add_AddsUserToDatabase()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "newuser@test.com",
            UserName = "newuser",
            PasswordHash = _passwordHash
        };

        // Act
        _repository.Add(user);
        await _context.SaveChangesAsync();

        // Assert
        var savedUser = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(savedUser);
        Assert.Equal(user.Email, savedUser.Email);
    }

    [Fact]
    public async Task Remove_RemovesUserFromDatabase()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "toremove@test.com",
            UserName = "toremove",
            PasswordHash = _passwordHash
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        var userId = user.Id;

        // Act
        _repository.Remove(user);
        await _context.SaveChangesAsync();

        // Assert
        var deletedUser = await _context.Users.FindAsync(userId);
        Assert.Null(deletedUser);
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
