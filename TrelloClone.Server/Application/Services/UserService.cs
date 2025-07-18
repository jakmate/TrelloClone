using TrelloClone.Shared.DTOs;

public class UserService
{
    private readonly IUserRepository _users;
    private readonly IBoardRepository _boards;
    private readonly IBoardUserRepository _boardUsers;
    private readonly IUnitOfWork _uow;

    public UserService(
        IUserRepository users,
        IBoardRepository boards,
        IBoardUserRepository boardUsers,
        IUnitOfWork uow)
    {
        _users = users;
        _boards = boards;
        _boardUsers = boardUsers;
        _uow = uow;
    }

    public async Task AddUserToBoardAsync(Guid boardId, Guid userId)
    {
        // ensure both exist
        var board = await _boards.GetByIdAsync(boardId)
                    ?? throw new KeyNotFoundException("Board not found.");
        var user = await _users.GetByIdWithBoardsAsync(userId)
                    ?? throw new KeyNotFoundException("User not found.");

        if (await _boardUsers.ExistsAsync(boardId, userId))
            throw new InvalidOperationException("User already on board.");

        _boardUsers.Add(new BoardUser { BoardId = boardId, UserId = userId });
        await _uow.SaveChangesAsync();
    }

    public async Task<UserDto?> GetUserAsync(Guid userId)
    {
        var user = await _users.GetByIdWithBoardsAsync(userId);
        if (user == null) return null;

        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Boards = user.BoardUsers
                           .Select(bu => new BoardDto
                           {
                               Id = bu.Board.Id,
                               Name = bu.Board.Name
                           })
                           .ToList()
        };
    }
}
