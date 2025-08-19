using TrelloClone.Shared.DTOs;

public class BoardService
{
    private readonly IBoardRepository _boards;
    private readonly IBoardUserRepository _boardUsers;
    private readonly IUnitOfWork _uow;

    public BoardService(IBoardRepository boards, IBoardUserRepository boardUser, IUnitOfWork uow)
    {
        _boards = boards;
        _boardUsers = boardUser;
        _uow = uow;
    }

    public async Task<BoardDto> CreateBoardAsync(string name, Guid ownerId)
    {
        if (await _boards.ExistsWithNameAsync(name, ownerId))
            throw new InvalidOperationException("You already have a board with that name.");

        // Get next position
        var existingBoards = await _boards.GetAllByUserIdAsync(ownerId);
        var nextPosition = existingBoards.Any() ? existingBoards.Max(b => b.Position) + 1 : 0;

        var board = new Board
        {
            Name = name,
            Position = nextPosition,
            BoardUsers = new List<BoardUser> {
            new BoardUser {
                UserId = ownerId,
                PermissionLevel = PermissionLevel.Admin
            }
        }
        };
        _boards.Add(board);
        await _uow.SaveChangesAsync();
        return new BoardDto
        {
            Id = board.Id,
            Name = board.Name,
            Position = board.Position
        };
    }

    public async Task<BoardDto> UpdateBoardAsync(Guid boardId, string newName, Guid userId)
    {
        var board = await _boards.GetByIdAsync(boardId);
        if (board == null)
            throw new InvalidOperationException("Board not found.");
        bool isMember = await _boardUsers.ExistsAsync(boardId, userId);
        if (!isMember)
            throw new UnauthorizedAccessException("You don't have permission to modify this board.");
        if (board.Name != newName)
        {
            bool nameExists = await _boards.ExistsWithNameAsync(newName, userId);
            if (nameExists)
                throw new InvalidOperationException("You already have a board with that name.");
        }
        board.Name = newName;
        await _uow.SaveChangesAsync();
        return new BoardDto
        {
            Id = board.Id,
            Name = board.Name,
            Position = board.Position
        };
    }

    public async Task DeleteBoardAsync(Guid boardId)
    {
        var board = await _boards.GetByIdAsync(boardId)
                    ?? throw new KeyNotFoundException("Board not found.");
        _boards.Remove(board);
        await _uow.SaveChangesAsync();
    }

    public async Task<BoardDto?> GetBoardAsync(Guid boardId)
    {
        var board = await _boards.GetByIdAsync(boardId);
        if (board == null) return null;
        return new BoardDto
        {
            Id = board.Id,
            Name = board.Name,
            Position = board.Position
        };
    }

    public async Task<BoardDto[]?> GetAllBoardsAsync(Guid ownerId)
    {
        var boards = await _boards.GetAllByUserIdAsync(ownerId);
        if (boards == null || !boards.Any()) return null;

        return boards.Select(board => new BoardDto
        {
            Id = board.Id,
            Name = board.Name,
            Position = board.Position
        }).ToArray();
    }

    public async Task<PermissionLevel> GetUserPermissionAsync(Guid boardId, Guid userId)
    {
        return await _boardUsers.GetUserPermissionAsync(boardId, userId);
    }

    public async Task ReorderBoardsAsync(List<BoardPositionDto> positions, Guid userId)
    {
        foreach (var pos in positions)
        {
            bool isMember = await _boardUsers.ExistsAsync(pos.Id, userId);
            if (!isMember)
                throw new UnauthorizedAccessException($"No permission for board {pos.Id}");
        }

        await _boards.UpdatePositionsAsync(positions);
        await _uow.SaveChangesAsync();
    }

    public async Task<BoardDto> CreateBoardFromTemplateAsync(CreateBoardFromTemplateRequest request)
    {
        if (await _boards.ExistsWithNameAsync(request.Name, request.OwnerId))
            throw new InvalidOperationException("You already have a board with that name.");

        var existingBoards = await _boards.GetAllByUserIdAsync(request.OwnerId);
        var nextPosition = 0;
        foreach (var existingBoard in existingBoards)
        {
            existingBoard.Position++;
        }

        var board = new Board
        {
            Name = request.Name,
            Position = nextPosition,
            BoardUsers = new List<BoardUser> {
            new BoardUser {
                UserId = request.OwnerId,
                PermissionLevel = PermissionLevel.Admin
            }
        },
            Columns = request.Columns.Select(col => new Column
            {
                Title = col.Title,
                Position = col.Position,
                Tasks = col.Tasks.Select(task => new TaskItem
                {
                    Name = task.Name,
                    Priority = task.Priority,
                    Position = task.Position
                }).ToList()
            }).ToList()
        };

        _boards.Add(board);
        await _uow.SaveChangesAsync();

        return new BoardDto
        {
            Id = board.Id,
            Name = board.Name,
            Position = board.Position
        };
    }
}