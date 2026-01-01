namespace TrelloClone.Shared.DTOs.Task;

public class TaskPositionDto
{
    public Guid Id { get; set; }
    public int Position { get; set; }
    public Guid? ColumnId { get; set; }
}
