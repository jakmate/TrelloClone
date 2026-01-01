using TrelloClone.Shared.Enums;

namespace TrelloClone.Shared.DTOs.Invitation;

public class CreateInvitationRequest
{
    public string? Username { get; set; }
    public PermissionLevel PermissionLevel { get; set; }
}
