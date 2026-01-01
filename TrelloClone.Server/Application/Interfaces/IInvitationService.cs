using TrelloClone.Shared.DTOs.Invitation;
using TrelloClone.Shared.Enums;

namespace TrelloClone.Server.Application.Interfaces;

public interface IInvitationService
{
    Task SendInvitation(Guid boardId, Guid inviterId, string invitedUsername, PermissionLevel permission);
    Task AcceptInvitation(Guid invitationId, Guid userId);
    Task DeclineInvitation(Guid invitationId, Guid userId);
    Task<List<BoardInvitationDto>> GetPendingInvitations(Guid userId);
}
