namespace TrelloClone.Shared.DTOs;

public class TaskDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public PriorityLevel Priority { get; set; }
    public List<Guid> AssignedUserIds { get; set; } = new List<Guid>();
    public Guid ColumnId { get; set; }
    public int Position { get; set; }
}