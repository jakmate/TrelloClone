using TrelloClone.Server.Domain.Entities;
using TrelloClone.Server.Domain.Interfaces;
using TrelloClone.Shared.DTOs.Board;
using TrelloClone.Shared.DTOs.Task;
using TrelloClone.Shared.Enums;

namespace TrelloClone.Server.Application.Services;

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
        if (await _boards.NameExistsAsync(name, ownerId))
        {
            throw new InvalidOperationException("You already have a board with that name.");
        }

        // Get next position
        var existingBoards = await _boards.GetAllByUserIdAsync(ownerId);
        var nextPosition = existingBoards.Count != 0 ? existingBoards.Max(b => b.Position) + 1 : 0;

        var board = new Board
        {
            Name = name,
            Position = nextPosition,
            BoardUsers = new List<BoardUser> {
            new BoardUser {
                UserId = ownerId,
                PermissionLevel = PermissionLevel.Owner
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
        var board = await _boards.GetByIdAsync(boardId)
           ?? throw new KeyNotFoundException("Board not found.");
        bool isMember = await _boardUsers.ExistsAsync(boardId, userId);
        if (!isMember)
        {
            throw new UnauthorizedAccessException("You don't have permission to modify this board.");
        }

        if (board.Name != newName)
        {
            bool nameExists = await _boards.NameExistsAsync(newName, userId);
            if (nameExists)
            {
                throw new InvalidOperationException("You already have a board with that name.");
            }
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

    public async Task DeleteBoardAsync(Guid boardId, Guid userId)
    {
        var board = await _boards.GetByIdAsync(boardId)
                    ?? throw new KeyNotFoundException("Board not found.");

        bool isOwner = await _boardUsers.IsOwnerAsync(boardId, userId);
        if (!isOwner)
        {
            throw new UnauthorizedAccessException("Only board owners can delete boards.");
        }

        _boards.Remove(board);
        await _uow.SaveChangesAsync();
    }

    public async Task LeaveBoardAsync(Guid boardId, Guid userId)
    {
        bool isOwner = await _boardUsers.IsOwnerAsync(boardId, userId);
        if (isOwner)
        {
            throw new InvalidOperationException("Board owners cannot leave their boards. Transfer ownership or delete the board instead.");
        }

        bool isMember = await _boardUsers.ExistsAsync(boardId, userId);
        if (!isMember)
        {
            throw new InvalidOperationException("You are not a member of this board.");
        }

        await _boardUsers.RemoveUserAsync(boardId, userId);
        await _uow.SaveChangesAsync();
    }

    public async Task<bool> IsOwnerAsync(Guid boardId, Guid userId)
    {
        return await _boardUsers.IsOwnerAsync(boardId, userId);
    }

    public async Task<BoardDto?> GetBoardAsync(Guid boardId)
    {
        var board = await _boards.GetByIdAsync(boardId);
        if (board == null)
        {
            return null;
        }

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
        if (boards == null || boards.Count == 0)
        {
            return null;
        }

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
            {
                throw new UnauthorizedAccessException($"No permission for board {pos.Id}");
            }
        }

        await _boards.UpdatePositionsAsync(positions);
        await _uow.SaveChangesAsync();
    }

    public async Task<BoardDto> CreateBoardFromTemplateAsync(CreateBoardFromTemplateRequest request)
    {
        if (await _boards.NameExistsAsync(request.Name, request.OwnerId))
        {
            throw new InvalidOperationException("You already have a board with that name.");
        }

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
                PermissionLevel = PermissionLevel.Owner
            }
        },
            Columns = request.Columns.Select(col => new Column
            {
                Title = col.Title,
                Position = col.Position,
                Tasks = (col.Tasks ?? new List<CreateTaskRequest>()).Select(task => new TaskItem
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
