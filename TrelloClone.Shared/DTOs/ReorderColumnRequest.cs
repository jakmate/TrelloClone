namespace TrelloClone.Shared.DTOs;

public class ReorderColumnsRequest
{
    public List<ColumnPositionDto> Columns { get; set; } = new();
}

public class ColumnPositionDto
{
    public Guid Id { get; set; }
    public int Position { get; set; }
}
