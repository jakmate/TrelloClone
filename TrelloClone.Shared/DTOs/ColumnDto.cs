namespace TrelloClone.Shared.DTOs;

public class ColumnDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public int Position { get; set; }

    public Guid BoardId { get; set; }
    public List<TaskDto>? Tasks { get; set; }
}