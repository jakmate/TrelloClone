using TrelloClone.Shared.DTOs;

public interface IBoardInvitationRepository
{
    Task<BoardInvitation?> GetByIdAsync(Guid id);
    void Add(BoardInvitation invitation);
    Task<List<BoardInvitationDto>> GetPendingInvitations(Guid userId);
    Task<BoardInvitation?> GetPendingInvitationAsync(Guid boardId, Guid userId);
}