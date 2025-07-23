public class SendInvitationDto
{
    public Guid BoardId { get; set; }
    public string Username { get; set; }
    public PermissionLevel Permission { get; set; }
}