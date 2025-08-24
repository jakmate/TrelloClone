using Microsoft.EntityFrameworkCore;

public class BoardRepository : IBoardRepository
{
    private readonly AppDbContext _ctx;
    public BoardRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task<bool> ExistsWithNameAsync(string name, Guid userId) =>
        await _ctx.Boards
            .AnyAsync(b => b.Name == name
                        && b.BoardUsers.Any(bu => bu.UserId == userId));

    public async Task<Board?> GetByIdAsync(Guid boardId) =>
        await _ctx.Boards
            .Include(b => b.Columns)
                .ThenInclude(c => c.Tasks)
            .FirstOrDefaultAsync(b => b.Id == boardId);

    public async Task<Board?> GetByIdWithMembersAsync(Guid boardId) =>
        await _ctx.Boards
            .Include(b => b.BoardUsers)
                .ThenInclude(bu => bu.User)
            .FirstOrDefaultAsync(b => b.Id == boardId);

    public async Task<List<Board>> GetAllByUserIdAsync(Guid userId) =>
        await _ctx.Boards
            .Include(b => b.Columns)
                .ThenInclude(c => c.Tasks)
            .Where(b => b.BoardUsers.Any(bu => bu.UserId == userId))
            .OrderBy(b => b.Position)
            .ToListAsync();

    public async Task UpdatePositionsAsync(List<BoardPositionDto> positions)
    {
        var boardIds = positions.Select(p => p.Id).ToList();
        var boards = await _ctx.Boards.Where(b => boardIds.Contains(b.Id)).ToListAsync();

        foreach (var pos in positions)
        {
            var board = boards.FirstOrDefault(b => b.Id == pos.Id);
            if (board != null)
            {
                board.Position = pos.Position;
            }
        }
    }

    public void Add(Board board) =>
        _ctx.Boards.Add(board);

    public void Update(Board board) =>
        _ctx.Boards.Update(board);

    public void Remove(Board board) =>
        _ctx?.Boards.Remove(board);
}