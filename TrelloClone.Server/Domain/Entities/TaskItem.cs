using TrelloClone.Shared.Enums;

namespace TrelloClone.Server.Domain.Entities;

public class TaskItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public PriorityLevel Priority { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Who it’s assigned to
    public ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
    public ICollection<User> AssignedUsers { get; set; } = new List<User>();

    // Column
    public Guid ColumnId { get; set; }
    public Column Column { get; set; } = null!;
    public int Position { get; set; }
}
