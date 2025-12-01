namespace TrelloClone.Server.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;

    // Navigation properties
    public ICollection<BoardUser> BoardUsers { get; set; } = new List<BoardUser>();
    public ICollection<BoardInvitation> SentInvitations { get; set; } = new List<BoardInvitation>();
    public ICollection<BoardInvitation> ReceivedInvitations { get; set; } = new List<BoardInvitation>();
    public ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
}
