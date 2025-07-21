using TrelloClone.Shared.DTOs;

public class BoardUser
{
    public Guid  BoardId { get; set; }
    public Board Board   { get; set; } = null!;

    public Guid  UserId  { get; set; }
    public User  User    { get; set; } = null!;
    public PermissionLevel PermissionLevel { get; set; }
}
