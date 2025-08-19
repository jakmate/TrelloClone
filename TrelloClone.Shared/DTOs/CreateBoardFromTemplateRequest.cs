namespace TrelloClone.Shared.DTOs;

public class CreateBoardFromTemplateRequest
{
    public string Name { get; set; } = null!;
    public Guid OwnerId { get; set; }
    public int TemplateId { get; set; }
    public List<CreateColumnRequest> Columns { get; set; } = new();
}