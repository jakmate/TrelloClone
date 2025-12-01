namespace TrelloClone.Shared.DTOs.SignalR;

public class TaskMoveInfo
{
    public string TaskId { get; set; } = string.Empty;
    public string FromColumnId { get; set; } = string.Empty;
    public string ToColumnId { get; set; } = string.Empty;
    public int NewPosition { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}
