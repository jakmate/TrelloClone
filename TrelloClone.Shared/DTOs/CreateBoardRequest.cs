namespace TrelloClone.Shared.DTOs;

public class CreateBoardRequest
{
    public string Name { get; set; } = null!;
    public Guid OwnerId { get; set; }
}