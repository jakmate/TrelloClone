namespace TrelloClone.Shared.DTOs;

public class ReorderTasksRequest
{
    public List<TaskPositionDto> Tasks { get; set; } = new();
}

public class TaskPositionDto
{
    public Guid Id { get; set; }
    public int Position { get; set; }
    public Guid? ColumnId { get; set; }
}
