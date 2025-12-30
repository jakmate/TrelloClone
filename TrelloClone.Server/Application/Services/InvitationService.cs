using Microsoft.AspNetCore.SignalR;

using TrelloClone.Server.Application.Hubs;
using TrelloClone.Server.Domain.Entities;
using TrelloClone.Server.Domain.Interfaces;
using TrelloClone.Shared.DTOs;

namespace TrelloClone.Server.Application.Services;

public class InvitationService
{
    private readonly IUserRepository _users;
    private readonly IBoardRepository _boards;
    private readonly IBoardUserRepository _boardUsers;
    private readonly IBoardInvitationRepository _boardInvitations;
    private readonly IUnitOfWork _uow;
    private readonly IHubContext<BoardHub> _hubContext;

    public InvitationService(
        IUserRepository users,
        IBoardRepository boards,
        IBoardUserRepository boardUsers,
        IBoardInvitationRepository boardInvitations,
        IUnitOfWork uow,
        IHubContext<BoardHub> hubContext)
    {
        _users = users;
        _boards = boards;
        _boardUsers = boardUsers;
        _boardInvitations = boardInvitations;
        _uow = uow;
        _hubContext = hubContext;
    }

    public async Task SendInvitation(Guid boardId, Guid inviterId, string invitedUsername, PermissionLevel permission)
    {
        var invitedUser = await _users.GetByUsernameAsync(invitedUsername);
        if (invitedUser == null)
        {
            throw new KeyNotFoundException($"User '{invitedUsername}' not found");
        }

        var existingMember = await _boardUsers.ExistsAsync(boardId, invitedUser.Id);
        if (existingMember)
        {
            throw new InvalidOperationException("User is already a member of this board");
        }

        var existingInvitation = await _boardInvitations.GetPendingInvitationAsync(boardId, invitedUser.Id);
        if (existingInvitation != null)
        {
            throw new InvalidOperationException("User already has a pending invitation for this board");
        }

        var invitation = new BoardInvitation
        {
            BoardId = boardId,
            InvitedUserId = invitedUser.Id,
            InviterUserId = inviterId,
            PermissionLevel = permission
        };

        _boardInvitations.Add(invitation);
        await _uow.SaveChangesAsync();

        var board = await _boards.GetByIdAsync(boardId);
        var inviter = await _users.GetByIdAsync(inviterId);
        var invitationDto = new BoardInvitationDto
        {
            Id = invitation.Id,
            BoardId = invitation.BoardId,
            BoardName = board?.Name ?? "Unknown Board",
            InviterName = inviter?.UserName ?? "Unknown User",
            SentAt = invitation.SentAt
        };

        try
        {
            await _hubContext.Clients.User(invitedUser.Id.ToString())
                .SendAsync("ReceiveInvitation", invitationDto);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignalR notification failed: {ex.Message}");
        }
    }

    public async Task AcceptInvitation(Guid invitationId, Guid userId)
    {
        var invitation = await _boardInvitations.GetByIdAsync(invitationId);
        if (invitation == null || invitation.InvitedUserId != userId)
        {
            throw new UnauthorizedAccessException();
        }

        if (invitation.Status != InvitationStatus.Pending)
        {
            throw new InvalidOperationException("Invitation is no longer pending");
        }

        var boardUser = new BoardUser
        {
            BoardId = invitation.BoardId,
            UserId = userId,
            PermissionLevel = invitation.PermissionLevel
        };

        _boardUsers.Add(boardUser);
        invitation.Status = InvitationStatus.Accepted;
        await _uow.SaveChangesAsync();

        await _hubContext.Clients.Group(invitation.BoardId.ToString())
            .SendAsync("UserJoinedBoard", new { BoardId = invitation.BoardId, UserId = userId });
    }

    public async Task DeclineInvitation(Guid invitationId, Guid userId)
    {
        var invitation = await _boardInvitations.GetByIdAsync(invitationId);
        if (invitation == null || invitation.InvitedUserId != userId)
        {
            throw new UnauthorizedAccessException();
        }

        if (invitation.Status != InvitationStatus.Pending)
        {
            throw new InvalidOperationException("Invitation is no longer pending");
        }

        invitation.Status = InvitationStatus.Rejected;
        await _uow.SaveChangesAsync();
    }

    public async Task<List<BoardInvitationDto>> GetPendingInvitations(Guid userId)
    {
        return await _boardInvitations.GetPendingInvitations(userId);
    }
}
