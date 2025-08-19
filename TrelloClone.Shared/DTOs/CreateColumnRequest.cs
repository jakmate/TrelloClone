namespace TrelloClone.Shared.DTOs;

public class CreateColumnRequest
{
    public string Title { get; set; } = null!;
    public int Position { get; set; }
    public Guid BoardId { get; set; }
    public List<CreateTaskRequest>? Tasks { get; set; } = new();
}