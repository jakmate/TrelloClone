using Microsoft.EntityFrameworkCore;

using TrelloClone.Server.Domain.Entities;
using TrelloClone.Server.Domain.Interfaces;
using TrelloClone.Shared.DTOs.Column;

namespace TrelloClone.Server.Infrastructure.Persistence;

public class ColumnRepository : IColumnRepository
{
    private readonly AppDbContext _ctx;
    public ColumnRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task<bool> ExistsWithTitleAsync(Guid boardId, string title) =>
        await _ctx.Columns.AnyAsync(c => c.BoardId == boardId && c.Title == title);

    public async Task<Column?> GetByIdAsync(Guid columnId) =>
        await _ctx.Columns
                  .Include(c => c.Board)
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
        var columnIds = positions.Select(p => p.Id).ToList();
        var columns = await _ctx.Columns.Where(c => columnIds.Contains(c.Id)).ToListAsync();

        foreach (var pos in positions)
        {
            var column = columns.FirstOrDefault(c => c.Id == pos.Id);
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
