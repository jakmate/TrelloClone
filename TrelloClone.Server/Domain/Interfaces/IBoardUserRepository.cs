using TrelloClone.Shared.DTOs;

public interface IBoardUserRepository
{
    void Add(BoardUser boardUser);
    Task<bool> ExistsAsync(Guid boardId, Guid userId);
    Task<PermissionLevel> GetUserPermissionAsync(Guid boardId, Guid userId);
    Task<bool> IsOwnerAsync(Guid boardId, Guid userId);
    Task RemoveUserAsync(Guid boardId, Guid userId);
}