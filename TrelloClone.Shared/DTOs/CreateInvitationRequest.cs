namespace TrelloClone.Shared.DTOs;

public class CreateInvitationRequest
{
    public string? Username { get; set; }
    public PermissionLevel PermissionLevel { get; set; }
}