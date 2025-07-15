using Microsoft.EntityFrameworkCore;

public class BoardRepository : IBoardRepository
{
    private readonly AppDbContext _ctx;
    public BoardRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task<bool> ExistsWithNameAsync(string name, Guid ownerId) =>
        await _ctx.Boards
            .AnyAsync(b => b.Name == name
                        && b.BoardUsers.Any(bu => bu.UserId == ownerId));

    public void Add(Board board) =>
        _ctx.Boards.Add(board);

    public async Task<Board?> GetByIdWithColumnsAsync(Guid id) =>
        await _ctx.Boards
            .Include(b => b.Columns)
                .ThenInclude(c => c.Tasks)
            .FirstOrDefaultAsync(b => b.Id == id);
}