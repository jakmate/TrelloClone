using TrelloClone.Shared.DTOs.User;

namespace TrelloClone.Shared.DTOs.Auth;

public class CurrentUserResponse
{
    public UserDto User { get; set; } = new();
}
