using TrelloClone.Shared.Enums;

namespace TrelloClone.Shared.DTOs.Task;

public class CreateTaskRequest
{
    public string Name { get; set; } = null!;
    public PriorityLevel Priority { get; set; }
    public Guid ColumnId { get; set; }
    public int Position { get; set; }
    public List<Guid>? AssignedUserIds { get; set; }
}
