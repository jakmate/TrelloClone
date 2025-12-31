using TrelloClone.Shared.DTOs;

namespace TrelloClone.Server.Application.Interfaces;

public interface IUserService
{
    Task AddUserToBoardAsync(Guid boardId, Guid userId);
    Task<UserDto?> GetUserAsync(Guid userId);
}
