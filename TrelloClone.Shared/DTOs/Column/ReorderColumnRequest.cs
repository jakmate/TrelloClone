namespace TrelloClone.Shared.DTOs.Column;

public class ReorderColumnsRequest
{
    public List<ColumnPositionDto> Columns { get; set; } = new();
}
