public interface IColumnRepository
{
    Task<bool> ExistsWithTitleAsync(Guid boardId, string title);
    void Add(Column column);
    Task<Column?> GetByIdAsync(Guid columnId);
    Task<List<Column>> ListByBoardAsync(Guid boardId);
    void Remove(Column column);
}