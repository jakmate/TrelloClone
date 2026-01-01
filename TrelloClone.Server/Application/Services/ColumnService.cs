using TrelloClone.Server.Domain.Entities;
using TrelloClone.Server.Domain.Interfaces;
using TrelloClone.Shared.DTOs.Column;
using TrelloClone.Shared.DTOs.Task;

namespace TrelloClone.Server.Application.Services;

public class ColumnService
{
    private readonly IColumnRepository _columns;
    private readonly IBoardRepository _boards;
    private readonly IUnitOfWork _uow;

    public ColumnService(
        IColumnRepository columns,
        IBoardRepository boards,
        IUnitOfWork uow)
    {
        _columns = columns;
        _boards = boards;
        _uow = uow;
    }

    public async Task<List<ColumnDto>> GetColumnsForBoardAsync(Guid boardId)
    {
        var list = await _columns.ListByBoardAsync(boardId);
        return list.OrderBy(c => c.Position) // Add ordering
            .Select(c => new ColumnDto
            {
                Id = c.Id,
                Title = c.Title,
                Position = c.Position,
                BoardId = c.BoardId,
                Tasks = c.Tasks.OrderBy(t => t.Position).Select(t => new TaskDto // Order tasks too
                {
                    Id = t.Id,
                    Name = t.Name,
                    Priority = t.Priority,
                    AssignedUserIds = t.AssignedUsers.Select(u => u.Id).ToList(),
                    ColumnId = t.ColumnId,
                    Position = t.Position
                }).ToList()
            }).ToList();
    }

    public async Task<ColumnDto> CreateColumnAsync(CreateColumnRequest req)
    {
        if (!await _boards.ExistsAsync(req.BoardId))
        {
            throw new KeyNotFoundException("Board not found.");
        }
        if (await _columns.ExistsWithTitleAsync(req.BoardId, req.Title))
        {
            throw new InvalidOperationException("Column title already in use.");
        }

        // Get next position
        var existingColumns = await _columns.ListByBoardAsync(req.BoardId);
        var nextPosition = existingColumns.Count != 0 ? existingColumns.Max(c => c.Position) + 1 : 0;

        var column = new Column
        {
            Title = req.Title,
            Position = nextPosition, // Use calculated position
            BoardId = req.BoardId
        };
        _columns.Add(column);
        await _uow.SaveChangesAsync();
        return new ColumnDto
        {
            Id = column.Id,
            Title = column.Title,
            Position = column.Position,
            BoardId = column.BoardId,
            Tasks = new List<TaskDto>()
        };
    }

    public async Task<ColumnDto> UpdateColumnAsync(Guid boardId, Guid columnId, UpdateColumnRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
        {
            throw new InvalidOperationException("Column title cannot be empty.");
        }

        var column = await _columns.GetByIdAsync(columnId);
        if (column == null)
        {
            throw new InvalidOperationException("Column not found.");
        }

        if (column.Title != req.Title)
        {
            bool nameExists = await _columns.ExistsWithTitleAsync(boardId, req.Title);
            if (nameExists)
            {
                throw new InvalidOperationException("You already have a column with that name.");
            }
        }

        column.Title = req.Title;
        await _uow.SaveChangesAsync();

        return new ColumnDto
        {
            Id = column.Id,
            Title = column.Title
        };
    }

    public async Task DeleteColumnAsync(Guid columnId)
    {
        var column = await _columns.GetByIdAsync(columnId)
                    ?? throw new KeyNotFoundException("Column not found.");
        _columns.Remove(column);
        await _uow.SaveChangesAsync();
    }

    public async Task ReorderColumnsAsync(Guid boardId, List<ColumnPositionDto> positions)
    {
        await _columns.UpdatePositionsAsync(positions);
        await _uow.SaveChangesAsync();
    }
}
