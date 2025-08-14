namespace TrelloClone.Shared.DTOs.SignalR;

public class UserStopEditInfo
{
    public string UserId { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
}