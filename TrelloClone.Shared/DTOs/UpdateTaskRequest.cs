namespace TrelloClone.Shared.DTOs;

public class UpdateTaskRequest
{
    public string? Name { get; set; }
    public PriorityLevel? Priority { get; set; }
    public List<Guid> AssignedUserIds { get; set; } = new List<Guid>();
}
