using TrelloClone.Shared.DTOs;

public interface IBoardUserRepository
{
    void Add(BoardUser boardUser);
    Task<bool> ExistsAsync(Guid boardId, Guid userId);
    Task<PermissionLevel> GetUserPermissionAsync(Guid boardId, Guid userId);
}