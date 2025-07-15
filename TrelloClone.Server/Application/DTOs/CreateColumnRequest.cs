public class CreateColumnRequest
{
    public string Title { get; set; } = null!;
    public int Position { get; set; }
    public Guid BoardId { get; set; }
}