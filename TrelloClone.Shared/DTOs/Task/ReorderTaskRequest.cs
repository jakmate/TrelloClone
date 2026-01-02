namespace TrelloClone.Shared.DTOs.Task;

public class ReorderTasksRequest
{
    public List<TaskPositionDto> Tasks { get; set; } = new();
}
