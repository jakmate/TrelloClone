namespace TrelloClone.Shared.DTOs.SignalR;

public class UserBoardEvent
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}