using TrelloClone.Shared.DTOs.Task;
using TrelloClone.Shared.DTOs.User;

namespace TrelloClone.Server.Application.Interfaces;

public interface ITaskService
{
    Task<List<TaskDto>> GetTasksForColumnAsync(Guid columnId);
    Task<TaskDto> CreateTaskAsync(CreateTaskRequest req);
    Task<TaskDto> UpdateTaskAsync(Guid taskId, UpdateTaskRequest req);
    Task DeleteTaskAsync(Guid taskId);
    Task ReorderTasksAsync(List<TaskPositionDto> positions);
    Task<List<UserDto>> GetAvailableUsersForTaskAsync(Guid columnId);
}
