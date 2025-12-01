namespace TrelloClone.Server.Domain.Entities;

public class Column
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public int Position { get; set; }

    // Foreign key + nav
    public Guid BoardId { get; set; }
    public Board Board { get; set; } = null!;

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
