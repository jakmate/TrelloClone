namespace TrelloClone.Shared.DTOs.Board;

public class CreateBoardRequest
{
    public string Name { get; set; } = null!;
    public Guid OwnerId { get; set; }
}
