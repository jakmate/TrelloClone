using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;

using TrelloClone.Server.Domain.Entities;
using TrelloClone.Server.Infrastructure.Persistence;

using Xunit;

namespace TrelloClone.Server.Tests.Infrastructure.Persistence;

public sealed class BoardInvitationRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly BoardInvitationRepository _repository;
    private bool _disposed;

    public BoardInvitationRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new BoardInvitationRepository(_context);
    }

    [Fact]
    public async Task GetByIdAsync_InvitationExists_ReturnsInvitation()
    {
        // Arrange
        var invitation = new BoardInvitation
        {
            Id = Guid.NewGuid(),
            BoardId = Guid.NewGuid(),
            InvitedUserId = Guid.NewGuid(),
            InviterUserId = Guid.NewGuid(),
            Status = InvitationStatus.Pending
        };
        _context.BoardInvitation.Add(invitation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(invitation.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(invitation.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_InvitationDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPendingInvitations_ReturnsPendingInvitationsForUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        var boardId = Guid.NewGuid();

        _context.Users.AddRange(
            new User { Id = userId, Email = "user@test.com", UserName = "user", PasswordHash = "dummy_hash" },
            new User { Id = inviterId, Email = "inviter@test.com", UserName = "inviter", PasswordHash = "dummy_hash" }
        );
        _context.Boards.Add(new Board { Id = boardId, Name = "Test Board" });
        _context.BoardInvitation.AddRange(
            new BoardInvitation
            {
                Id = Guid.NewGuid(),
                BoardId = boardId,
                InvitedUserId = userId,
                InviterUserId = inviterId,
                Status = InvitationStatus.Pending,
                SentAt = DateTime.UtcNow
            },
            new BoardInvitation
            {
                Id = Guid.NewGuid(),
                BoardId = boardId,
                InvitedUserId = userId,
                InviterUserId = inviterId,
                Status = InvitationStatus.Accepted,
                SentAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPendingInvitations(userId);

        // Assert
        Assert.Single(result);
        Assert.Equal("Test Board", result[0].BoardName);
        Assert.Equal("inviter", result[0].InviterName);
    }

    [Fact]
    public async Task GetPendingInvitations_NoInvitations_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetPendingInvitations(Guid.NewGuid());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPendingInvitationAsync_PendingInvitationExists_ReturnsInvitation()
    {
        // Arrange
        var boardId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var invitation = new BoardInvitation
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            InvitedUserId = userId,
            InviterUserId = Guid.NewGuid(),
            Status = InvitationStatus.Pending
        };
        _context.BoardInvitation.Add(invitation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPendingInvitationAsync(boardId, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(invitation.Id, result.Id);
    }

    [Fact]
    public async Task GetPendingInvitationAsync_AcceptedInvitation_ReturnsNull()
    {
        // Arrange
        var boardId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _context.BoardInvitation.Add(new BoardInvitation
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            InvitedUserId = userId,
            InviterUserId = Guid.NewGuid(),
            Status = InvitationStatus.Accepted
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPendingInvitationAsync(boardId, userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Add_AddsInvitationToContext()
    {
        // Arrange
        var invitation = new BoardInvitation
        {
            Id = Guid.NewGuid(),
            BoardId = Guid.NewGuid(),
            InvitedUserId = Guid.NewGuid(),
            InviterUserId = Guid.NewGuid(),
            Status = InvitationStatus.Pending
        };

        // Act
        _repository.Add(invitation);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await _context.BoardInvitation.FindAsync(invitation.Id);
        Assert.NotNull(saved);
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
