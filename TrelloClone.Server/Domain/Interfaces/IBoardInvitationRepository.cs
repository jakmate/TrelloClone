using TrelloClone.Server.Domain.Entities;
using TrelloClone.Shared.DTOs;

namespace TrelloClone.Server.Domain.Interfaces;

public interface IBoardInvitationRepository
{
    Task<BoardInvitation?> GetByIdAsync(Guid id);
    void Add(BoardInvitation invitation);
    Task<List<BoardInvitationDto>> GetPendingInvitations(Guid userId);
    Task<BoardInvitation?> GetPendingInvitationAsync(Guid boardId, Guid userId);
}
