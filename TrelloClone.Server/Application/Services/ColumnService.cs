using TrelloClone.Shared.DTOs;

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
        return list.Select(c => new ColumnDto
        {
            Id = c.Id,
            Title = c.Title,
            Position = c.Position,
            BoardId = c.BoardId,
            Tasks = c.Tasks.Select(t => new TaskDto
            {
                Id = t.Id,
                Name = t.Name,
                Priority = t.Priority,
                AssignedUserId = t.AssignedUserId
            }).ToList()
        }).ToList();
    }

    public async Task<ColumnDto> CreateColumnAsync(CreateColumnRequest req)
    {
        var board = await _boards.GetByIdAsync(req.BoardId)
                    ?? throw new KeyNotFoundException("Board not found.");

        if (await _columns.ExistsWithTitleAsync(req.BoardId, req.Title))
            throw new InvalidOperationException("Column title already in use.");

        var column = new Column
        {
            Title = req.Title,
            Position = req.Position,
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
        var column = await _columns.GetByIdAsync(columnId);
        if (column == null)
            throw new InvalidOperationException("Column not found.");

        if (column.Title != req.Title)
        {
            bool nameExists = await _columns.ExistsWithTitleAsync(boardId, req.Title);
            if (nameExists)
                throw new InvalidOperationException("You already have a column with that name.");
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
}