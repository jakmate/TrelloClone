using Microsoft.EntityFrameworkCore;

public class ColumnRepository : IColumnRepository
{
    private readonly AppDbContext _ctx;
    public ColumnRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task<bool> ExistsWithTitleAsync(Guid boardId, string title) =>
        await _ctx.Columns.AnyAsync(c => c.BoardId == boardId && c.Title == title);

    public void Add(Column column) =>
        _ctx.Columns.Add(column);

    public async Task<Column?> GetByIdAsync(Guid columnId) =>
        await _ctx.Columns
                  .Include(c => c.Tasks)
                  .FirstOrDefaultAsync(c => c.Id == columnId);

    public async Task<List<Column>> ListByBoardAsync(Guid boardId) =>
        await _ctx.Columns
                  .Where(c => c.BoardId == boardId)
                  .OrderBy(c => c.Position)
                  .ToListAsync();

    public void Remove(Column column) =>
        _ctx.Columns.Remove(column);
}