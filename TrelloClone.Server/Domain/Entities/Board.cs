namespace TrelloClone.Server.Domain.Entities;

public class Board
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int Position { get; set; }
    public ICollection<Column> Columns { get; set; } = new List<Column>();
    public ICollection<BoardUser> BoardUsers { get; set; } = new List<BoardUser>();
    public ICollection<BoardInvitation> Invitations { get; set; } = new List<BoardInvitation>();
}
