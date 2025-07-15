public interface IBoardRepository
{
    Task<bool> ExistsWithNameAsync(string name, Guid ownerId);
    void Add(Board board);
    Task<Board?> GetByIdWithColumnsAsync(Guid id);
}