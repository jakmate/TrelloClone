namespace TrelloClone.Shared.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;

    public List<BoardDto> Boards { get; set; } = new();
}

public class CurrentUserResponse
{
    public UserDto User { get; set; } = new();
}
