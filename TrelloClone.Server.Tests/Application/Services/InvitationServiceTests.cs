using Microsoft.AspNetCore.SignalR;

using Moq;

using TrelloClone.Server.Application.Hubs;
using TrelloClone.Server.Application.Services;
using TrelloClone.Server.Domain.Entities;
using TrelloClone.Server.Domain.Interfaces;
using TrelloClone.Shared.DTOs;

using Xunit;

namespace TrelloClone.Server.Tests.Application.Services;

public class InvitationServiceTests
{
    private readonly Mock<IUserRepository> _mockUsers;
    private readonly Mock<IBoardRepository> _mockBoards;
    private readonly Mock<IBoardUserRepository> _mockBoardUsers;
    private readonly Mock<IBoardInvitationRepository> _mockInvitations;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IHubContext<BoardHub>> _mockHubContext;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly InvitationService _service;

    public InvitationServiceTests()
    {
        _mockUsers = new Mock<IUserRepository>();
        _mockBoards = new Mock<IBoardRepository>();
        _mockBoardUsers = new Mock<IBoardUserRepository>();
        _mockInvitations = new Mock<IBoardInvitationRepository>();
        _mockUow = new Mock<IUnitOfWork>();
        _mockHubContext = new Mock<IHubContext<BoardHub>>();
        _mockClientProxy = new Mock<IClientProxy>();

        _mockHubContext.Setup(x => x.Clients.User(It.IsAny<string>())).Returns(_mockClientProxy.Object);
        _mockHubContext.Setup(x => x.Clients.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);

        _service = new InvitationService(
            _mockUsers.Object,
            _mockBoards.Object,
            _mockBoardUsers.Object,
            _mockInvitations.Object,
            _mockUow.Object,
            _mockHubContext.Object);
    }

    [Fact]
    public async Task SendInvitation_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockUsers.Setup(x => x.GetByUsernameAsync("unknown")).ReturnsAsync((User?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.SendInvitation(Guid.NewGuid(), Guid.NewGuid(), "unknown", PermissionLevel.Viewer));
        Assert.Equal("User 'unknown' not found", ex.Message);
    }

    [Fact]
    public async Task SendInvitation_UserAlreadyMember_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var user = new User { Id = userId, UserName = "testuser" };

        _mockUsers.Setup(x => x.GetByUsernameAsync("testuser")).ReturnsAsync(user);
        _mockBoardUsers.Setup(x => x.ExistsAsync(boardId, userId)).ReturnsAsync(true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SendInvitation(boardId, Guid.NewGuid(), "testuser", PermissionLevel.Viewer));
        Assert.Equal("User is already a member of this board", ex.Message);
    }

    [Fact]
    public async Task SendInvitation_PendingInvitationExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var user = new User { Id = userId, UserName = "testuser" };
        var existingInvitation = new BoardInvitation { Id = Guid.NewGuid() };

        _mockUsers.Setup(x => x.GetByUsernameAsync("testuser")).ReturnsAsync(user);
        _mockBoardUsers.Setup(x => x.ExistsAsync(boardId, userId)).ReturnsAsync(false);
        _mockInvitations.Setup(x => x.GetPendingInvitationAsync(boardId, userId)).ReturnsAsync(existingInvitation);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SendInvitation(boardId, Guid.NewGuid(), "testuser", PermissionLevel.Viewer));
        Assert.Equal("User already has a pending invitation for this board", ex.Message);
    }

    [Fact]
    public async Task SendInvitation_ValidRequest_CreatesInvitationAndSendsNotification()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        var user = new User { Id = userId, UserName = "testuser" };
        var board = new Board { Id = boardId, Name = "Test Board" };
        var inviter = new User { Id = inviterId, UserName = "inviter" };

        _mockUsers.Setup(x => x.GetByUsernameAsync("testuser")).ReturnsAsync(user);
        _mockBoardUsers.Setup(x => x.ExistsAsync(boardId, userId)).ReturnsAsync(false);
        _mockInvitations.Setup(x => x.GetPendingInvitationAsync(boardId, userId)).ReturnsAsync((BoardInvitation?)null);
        _mockBoards.Setup(x => x.GetByIdAsync(boardId)).ReturnsAsync(board);
        _mockUsers.Setup(x => x.GetByIdAsync(inviterId)).ReturnsAsync(inviter);

        // Act
        await _service.SendInvitation(boardId, inviterId, "testuser", PermissionLevel.Editor);

        // Assert
        _mockInvitations.Verify(x => x.Add(It.Is<BoardInvitation>(i =>
            i.BoardId == boardId &&
            i.InvitedUserId == userId &&
            i.InviterUserId == inviterId &&
            i.PermissionLevel == PermissionLevel.Editor)), Times.Once);
        _mockUow.Verify(x => x.SaveChangesAsync(), Times.Once);
        _mockClientProxy.Verify(x => x.SendCoreAsync(
            "ReceiveInvitation",
            It.IsAny<object[]>(),
            default), Times.Once);
    }

    [Fact]
    public async Task SendInvitation_SignalRFails_ContinuesWithoutThrowing()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        var user = new User { Id = userId, UserName = "testuser" };
        var board = new Board { Id = boardId, Name = "Test Board" };
        var inviter = new User { Id = inviterId, UserName = "inviter" };

        _mockUsers.Setup(x => x.GetByUsernameAsync("testuser")).ReturnsAsync(user);
        _mockBoardUsers.Setup(x => x.ExistsAsync(boardId, userId)).ReturnsAsync(false);
        _mockInvitations.Setup(x => x.GetPendingInvitationAsync(boardId, userId)).ReturnsAsync((BoardInvitation?)null);
        _mockBoards.Setup(x => x.GetByIdAsync(boardId)).ReturnsAsync(board);
        _mockUsers.Setup(x => x.GetByIdAsync(inviterId)).ReturnsAsync(inviter);

        _mockClientProxy.Setup(x => x.SendCoreAsync(
            It.IsAny<string>(),
            It.IsAny<object[]>(),
            default)).ThrowsAsync(new Exception("SignalR error"));

        // Act
        await _service.SendInvitation(boardId, inviterId, "testuser", PermissionLevel.Editor);

        // Assert - should complete without throwing
        _mockInvitations.Verify(x => x.Add(It.IsAny<BoardInvitation>()), Times.Once);
        _mockUow.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AcceptInvitation_InvitationNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _mockInvitations.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((BoardInvitation?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.AcceptInvitation(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task AcceptInvitation_WrongUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var invitation = new BoardInvitation { Id = Guid.NewGuid(), InvitedUserId = Guid.NewGuid() };
        _mockInvitations.Setup(x => x.GetByIdAsync(invitation.Id)).ReturnsAsync(invitation);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.AcceptInvitation(invitation.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task AcceptInvitation_NotPending_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invitation = new BoardInvitation
        {
            Id = Guid.NewGuid(),
            InvitedUserId = userId,
            Status = InvitationStatus.Accepted
        };
        _mockInvitations.Setup(x => x.GetByIdAsync(invitation.Id)).ReturnsAsync(invitation);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AcceptInvitation(invitation.Id, userId));
        Assert.Equal("Invitation is no longer pending", ex.Message);
    }

    [Fact]
    public async Task AcceptInvitation_ValidRequest_AddsToBoardAndUpdatesStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var invitation = new BoardInvitation
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            InvitedUserId = userId,
            Status = InvitationStatus.Pending,
            PermissionLevel = PermissionLevel.Editor
        };
        _mockInvitations.Setup(x => x.GetByIdAsync(invitation.Id)).ReturnsAsync(invitation);

        // Act
        await _service.AcceptInvitation(invitation.Id, userId);

        // Assert
        _mockBoardUsers.Verify(x => x.Add(It.Is<BoardUser>(bu =>
            bu.BoardId == boardId &&
            bu.UserId == userId &&
            bu.PermissionLevel == PermissionLevel.Editor)), Times.Once);
        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
        _mockUow.Verify(x => x.SaveChangesAsync(), Times.Once);
        _mockClientProxy.Verify(x => x.SendCoreAsync(
            "UserJoinedBoard",
            It.IsAny<object[]>(),
            default), Times.Once);
    }

    [Fact]
    public async Task DeclineInvitation_InvitationNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _mockInvitations.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((BoardInvitation?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.DeclineInvitation(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task DeclineInvitation_ValidRequest_UpdatesStatusToRejected()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invitation = new BoardInvitation
        {
            Id = Guid.NewGuid(),
            InvitedUserId = userId,
            Status = InvitationStatus.Pending
        };
        _mockInvitations.Setup(x => x.GetByIdAsync(invitation.Id)).ReturnsAsync(invitation);

        // Act
        await _service.DeclineInvitation(invitation.Id, userId);

        // Assert
        Assert.Equal(InvitationStatus.Rejected, invitation.Status);
        _mockUow.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeclineInvitation_NotPending_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invitation = new BoardInvitation
        {
            Id = Guid.NewGuid(),
            InvitedUserId = userId,
            Status = InvitationStatus.Accepted
        };
        _mockInvitations.Setup(x => x.GetByIdAsync(invitation.Id)).ReturnsAsync(invitation);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeclineInvitation(invitation.Id, userId));
        Assert.Equal("Invitation is no longer pending", ex.Message);
    }

    [Fact]
    public async Task GetPendingInvitations_ReturnsInvitationsFromRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invitations = new List<BoardInvitationDto>
        {
            new BoardInvitationDto { Id = Guid.NewGuid(), BoardName = "Board 1" },
            new BoardInvitationDto { Id = Guid.NewGuid(), BoardName = "Board 2" }
        };
        _mockInvitations.Setup(x => x.GetPendingInvitations(userId)).ReturnsAsync(invitations);

        // Act
        var result = await _service.GetPendingInvitations(userId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(invitations, result);
    }
}
