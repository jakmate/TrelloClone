namespace TrelloClone.Client.Models;

public class CreateBoardRequest
{
    public string Name { get; set; } = null!;
    public Guid OwnerId { get; set; }
}