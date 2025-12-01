namespace TrelloClone.Shared.DTOs;

public class BoardInvitationDto
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public string BoardName { get; set; } = string.Empty;
    public string InviterName { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}
