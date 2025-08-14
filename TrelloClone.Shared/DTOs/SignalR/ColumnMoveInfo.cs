namespace TrelloClone.Shared.DTOs.SignalR;

public class ColumnMoveInfo
{
    public string ColumnId { get; set; } = string.Empty;
    public int NewPosition { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}