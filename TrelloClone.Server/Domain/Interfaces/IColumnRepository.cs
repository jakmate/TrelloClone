public interface IColumnRepository
{
    Task<bool> ExistsWithTitleAsync(Guid boardId, string title);
    Task<Column?> GetByIdAsync(Guid columnId);
    Task<List<Column>> ListByBoardAsync(Guid boardId);
    Task UpdatePositionsAsync(List<ColumnPositionDto> positions);
    void Add(Column column);
    void Update(Column column);
    void Remove(Column column);
}