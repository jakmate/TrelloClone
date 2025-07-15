public interface IBoardUserRepository
{
    void Add(BoardUser boardUser);
    Task<bool> ExistsAsync(Guid boardId, Guid userId);
}