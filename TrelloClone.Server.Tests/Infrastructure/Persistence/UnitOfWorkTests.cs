using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;

using Moq;

using TrelloClone.Server.Domain.Entities;
using TrelloClone.Server.Domain.Interfaces;
using TrelloClone.Server.Infrastructure.Persistence;

using Xunit;

namespace TrelloClone.Server.Tests.Infrastructure.Persistence;

public sealed class UnitOfWorkTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private bool _disposed;

    public UnitOfWorkTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
    }

    [Fact]
    public async Task SaveChangesAsync_SavesChangesToDatabase()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            UserName = "test",
            PasswordHash = "dummy_hash"
        };
        _context.Users.Add(user);

        // Act
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var savedUser = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(savedUser);
        Assert.Equal(user.Email, savedUser.Email);
    }

    [Fact]
    public async Task SaveChangesAsync_UpdatesExistingEntity()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            UserName = "test",
            PasswordHash = "dummy_hash"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var updatedEmail = "updated@test.com";
        user.Email = updatedEmail;

        // Act
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var savedUser = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(savedUser);
        Assert.Equal(updatedEmail, savedUser.Email);
    }

    [Fact]
    public async Task SaveChangesAsync_DeletesEntity()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            UserName = "test",
            PasswordHash = "dummy_hash"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var userId = user.Id;
        _context.Users.Remove(user);

        // Act
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var deletedUser = await _context.Users.FindAsync(userId);
        Assert.Null(deletedUser);
    }

    [Fact]
    public async Task SaveChangesAsync_ThrowsException_WhenDbContextThrows()
    {
        // Arrange
        var mockContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
        mockContext.Setup(ctx => ctx.SaveChangesAsync(It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new DbUpdateException("Database error"));

        var unitOfWork = new UnitOfWork(mockContext.Object);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_CallsDbContextSaveChangesAsync()
    {
        // Arrange
        var mockContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
        var unitOfWork = new UnitOfWork(mockContext.Object);

        // Act
        await unitOfWork.SaveChangesAsync();

        // Assert
        mockContext.Verify(ctx => ctx.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

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
                _context?.Dispose();
            }
            _disposed = true;
        }
    }
}
