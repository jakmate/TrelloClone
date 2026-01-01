namespace TrelloClone.Shared.DTOs.Board;

public class BoardDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int Position { get; set; }
}
