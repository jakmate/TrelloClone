using Microsoft.EntityFrameworkCore;

public class BoardUserRepository : IBoardUserRepository
{
    private readonly AppDbContext _ctx;
    public BoardUserRepository(AppDbContext ctx) => _ctx = ctx;

    public void Add(BoardUser bu) => _ctx.BoardUsers.Add(bu);

    public async Task<bool> ExistsAsync(Guid boardId, Guid userId) =>
        await _ctx.BoardUsers.AnyAsync(bu => bu.BoardId == boardId && bu.UserId == userId);

    public async Task<PermissionLevel> GetUserPermissionAsync(Guid boardId, Guid userId)
    {
        var boardUser = await _ctx.BoardUsers
            .FirstOrDefaultAsync(bu => bu.BoardId == boardId && bu.UserId == userId);

        return boardUser?.PermissionLevel ?? PermissionLevel.Viewer;
    }

    public async Task<bool> IsOwnerAsync(Guid boardId, Guid userId)
    {
        var boardUser = await _ctx.BoardUsers
            .FirstOrDefaultAsync(bu => bu.BoardId == boardId && bu.UserId == userId);
        return boardUser?.PermissionLevel == PermissionLevel.Owner;
    }

    public async Task RemoveUserAsync(Guid boardId, Guid userId)
    {
        var boardUser = await _ctx.BoardUsers
            .FirstOrDefaultAsync(bu => bu.BoardId == boardId && bu.UserId == userId);
        if (boardUser != null)
        {
            _ctx.BoardUsers.Remove(boardUser);
        }
    }
}
