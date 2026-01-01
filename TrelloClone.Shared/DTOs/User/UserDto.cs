using TrelloClone.Shared.DTOs.Board;

namespace TrelloClone.Shared.DTOs.User;

public class UserDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;

    public List<BoardDto> Boards { get; set; } = new();
}
