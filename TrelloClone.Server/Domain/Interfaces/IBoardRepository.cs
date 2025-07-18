public interface IBoardRepository
{
    Task<bool> ExistsWithNameAsync(string name, Guid userId);
    Task<Board?> GetByIdAsync(Guid boardId);
    Task<List<Board>> GetAllByUserIdAsync(Guid userId);
    void Add(Board board);
    void Update(Board board);
    void Remove(Board board);
}