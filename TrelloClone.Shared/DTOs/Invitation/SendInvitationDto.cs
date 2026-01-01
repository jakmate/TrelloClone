using TrelloClone.Shared.Enums;

namespace TrelloClone.Shared.DTOs.Invitation;

public class SendInvitationDto
{
    public Guid BoardId { get; set; }
    public string? Username { get; set; }
    public PermissionLevel Permission { get; set; }
}
