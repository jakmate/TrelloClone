namespace TrelloClone.Shared.DTOs.SignalR;

public class TaskDeleteInfo
{
    public string TaskId { get; set; } = string.Empty;
    public string ColumnId { get; set; } = string.Empty;
}