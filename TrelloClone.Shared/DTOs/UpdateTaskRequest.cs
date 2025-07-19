namespace TrelloClone.Shared.DTOs;

public class UpdateTaskRequest
{
    public string Name { get; set; }
    public PriorityLevel? Priority { get; set; }
    public Guid? AssignedUserId { get; set; }
}