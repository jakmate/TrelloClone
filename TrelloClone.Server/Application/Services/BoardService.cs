public class BoardService
{
    private readonly IBoardRepository _boards;
    private readonly IUnitOfWork _uow;

    public BoardService(IBoardRepository boards, IUnitOfWork uow)
    {
        _boards = boards;
        _uow = uow;
    }

    public async Task<BoardDto> CreateBoardAsync(string name, Guid ownerId)
    {
        if (await _boards.ExistsWithNameAsync(name, ownerId))
            throw new InvalidOperationException("You already have a board with that name.");

        var board = new Board { Name = name };
        _boards.Add(board);
        await _uow.SaveChangesAsync(); // Save board first to generate ID

        board.BoardUsers.Add(new BoardUser { BoardId = board.Id, UserId = ownerId });
        await _uow.SaveChangesAsync(); // Save the relationship

        return new BoardDto
        {
            Id = board.Id,
            Name = board.Name,
            ColumnCount = 0
        };
    }

    public async Task<BoardDto?> GetBoardAsync(Guid id)
    {
        var board = await _boards.GetByIdWithColumnsAsync(id);
        if (board == null) return null;

        return new BoardDto
        {
            Id = board.Id,
            Name = board.Name,
            ColumnCount = board.Columns.Count
        };
    }
}