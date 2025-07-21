using TrelloClone.Server.Application.Hubs;
using Microsoft.AspNetCore.SignalR;
using TrelloClone.Shared.DTOs;

public class InvitationService
{
    private readonly IUserRepository _users;
    private readonly IBoardUserRepository _boardUsers;
    private readonly IBoardInvitationRepository _boardInvitations;
    private readonly IUnitOfWork _uow;
    private readonly IHubContext<BoardHub> _hubContext;

    public InvitationService(
        IUserRepository users,
        IBoardUserRepository boardUsers,
        IBoardInvitationRepository boardInvitations,
        IUnitOfWork uow,
        IHubContext<BoardHub> hubContext)
    {
        _users = users;
        _boardUsers = boardUsers;
        _boardInvitations = boardInvitations;
        _uow = uow;
        _hubContext = hubContext;
    }

    public async Task SendInvitation(Guid boardId, Guid inviterId, string invitedUsername, PermissionLevel permission)
    {
        var invitedUser = await _users.GetByUsernameAsync(invitedUsername);
        if (invitedUser == null) throw new Exception("User not found");

        var invitation = new BoardInvitation
        {
            BoardId = boardId,
            InvitedUserId = invitedUser.Id,
            InviterUserId = inviterId,
            PermissionLevel = permission
        };

        _boardInvitations.Add(invitation);
        await _uow.SaveChangesAsync();

        // Notify invited user
        await _hubContext.Clients.User(invitedUser.Id.ToString())
            .SendAsync("ReceiveInvitation", invitation);
    }

    public async Task AcceptInvitation(Guid invitationId, Guid userId)
    {
        var invitation = await _boardInvitations.GetByIdAsync(invitationId);
        if (invitation == null || invitation.InvitedUserId != userId)
            throw new UnauthorizedAccessException();

        // Add user to board
        var boardUser = new BoardUser
        {
            BoardId = invitation.BoardId,
            UserId = userId,
            PermissionLevel = invitation.PermissionLevel
        };

        _boardUsers.Add(boardUser);
        invitation.Status = InvitationStatus.Accepted;

        await _uow.SaveChangesAsync();

        // Notify board members
        await _hubContext.Clients.Group(invitation.BoardId.ToString())
            .SendAsync("UserJoinedBoard", new { BoardId = invitation.BoardId, UserId = userId });
    }

    public async Task<List<BoardInvitationDto>> GetPendingInvitations(Guid userId)
    {
        return await _boardInvitations.GetPendingInvitations(userId);
    }
}