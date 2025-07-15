public enum PriorityLevel { Low, Medium, High }

public class TaskItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public PriorityLevel Priority { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Who it’s assigned to (nullable)
    public Guid? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }

    // Column
    public Guid ColumnId { get; set; }
    public Column Column { get; set; } = null!;
}
