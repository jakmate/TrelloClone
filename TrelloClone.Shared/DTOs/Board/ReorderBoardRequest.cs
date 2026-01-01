namespace TrelloClone.Shared.DTOs.Board;

public class ReorderBoardsRequest
{
    public List<BoardPositionDto> Boards { get; set; } = new();
}
