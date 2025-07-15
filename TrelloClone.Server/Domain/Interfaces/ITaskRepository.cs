public interface ITaskRepository
{
    void Add(TaskItem task);
    Task<TaskItem?> GetByIdAsync(Guid taskId);
    Task<List<TaskItem>> ListByColumnAsync(Guid columnId);
    void Remove(TaskItem task);
}