public class TaskDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public PriorityLevel Priority { get; set; }
    public Guid? AssignedUserId { get; set; }
    public Guid ColumnId { get; set; }
}