using TrelloClone.Server.Domain.Entities;
using TrelloClone.Shared.DTOs.Task;

namespace TrelloClone.Server.Domain.Interfaces;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid taskId);
    Task<List<TaskItem>> ListByColumnAsync(Guid columnId);
    Task UpdatePositionsAsync(List<TaskPositionDto> positions);
    Task AssignUsersToTaskAsync(Guid taskId, List<Guid> userIds);
    void Add(TaskItem task);
    void Update(TaskItem task);
    void Remove(TaskItem task);
}
