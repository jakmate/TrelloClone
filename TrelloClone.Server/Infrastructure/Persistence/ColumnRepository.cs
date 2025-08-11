using Microsoft.EntityFrameworkCore;

public class ColumnRepository : IColumnRepository
{
    private readonly AppDbContext _ctx;
    public ColumnRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task<bool> ExistsWithTitleAsync(Guid boardId, string title) =>
        await _ctx.Columns.AnyAsync(c => c.BoardId == boardId && c.Title == title);

    public async Task<Column?> GetByIdAsync(Guid columnId) =>
        await _ctx.Columns
                  .Include(c => c.Tasks)
                  .FirstOrDefaultAsync(c => c.Id == columnId);

    public async Task<List<Column>> ListByBoardAsync(Guid boardId) =>
        await _ctx.Columns
            .Include(c => c.Tasks)
            .Where(c => c.BoardId == boardId)
            .OrderBy(c => c.Position)
            .ToListAsync();

    public async Task UpdatePositionsAsync(List<ColumnPositionDto> positions)
    {
        foreach (var pos in positions)
        {
            var column = await _ctx.Columns.FindAsync(pos.Id);
            if (column != null)
            {
                column.Position = pos.Position;
            }
        }
    }

    public void Add(Column column) =>
        _ctx.Columns.Add(column);

    public void Update(Column column) =>
        _ctx.Columns.Update(column);

    public void Remove(Column column) =>
        _ctx.Columns.Remove(column);
}