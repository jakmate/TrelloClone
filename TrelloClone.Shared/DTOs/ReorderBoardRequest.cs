public class ReorderBoardsRequest
{
    public List<BoardPositionDto> Boards { get; set; } = new();
}

public class BoardPositionDto
{
    public Guid Id { get; set; }
    public int Position { get; set; }
}