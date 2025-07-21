public enum InvitationStatus { Pending, Accepted, Rejected }

public class BoardInvitation
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public Board Board { get; set; } = null!;
    public Guid InvitedUserId { get; set; }
    public User InvitedUser { get; set; } = null!;
    public Guid InviterUserId { get; set; }
    public User InviterUser { get; set; } = null!;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    public PermissionLevel PermissionLevel { get; set; }
}