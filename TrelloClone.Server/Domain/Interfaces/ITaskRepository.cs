public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid taskId);
    Task<List<TaskItem>> ListByColumnAsync(Guid columnId);
    Task UpdatePositionsAsync(List<TaskPositionDto> positions);
    void Add(TaskItem task);
    void Update(TaskItem task);
    void Remove(TaskItem task);
}