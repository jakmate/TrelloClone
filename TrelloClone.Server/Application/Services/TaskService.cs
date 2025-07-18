using TrelloClone.Shared.DTOs;

public class TaskService
{
    private readonly ITaskRepository _tasks;
    private readonly IColumnRepository _columns;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;

    public TaskService(
        ITaskRepository tasks,
        IColumnRepository columns,
        IUserRepository users,
        IUnitOfWork uow)
    {
        _tasks = tasks;
        _columns = columns;
        _users = users;
        _uow = uow;
    }

    public async Task<TaskDto> CreateTaskAsync(CreateTaskRequest req)
    {
        // Ensure column exists
        var col = await _columns.GetByIdAsync(req.ColumnId)
                  ?? throw new KeyNotFoundException("Column not found.");

        // Map to domain
        var task = new TaskItem
        {
            Name = req.Name,
            Priority = req.Priority,
            AssignedUserId = req.AssignedUserId,
            ColumnId = req.ColumnId
        };
        _tasks.Add(task);

        // Persist
        await _uow.SaveChangesAsync();

        // Map back
        return new TaskDto
        {
            Id = task.Id,
            Name = task.Name,
            Priority = task.Priority,
            AssignedUserId = task.AssignedUserId,
            ColumnId = task.ColumnId
        };
    }

    public async Task<List<TaskDto>> GetTasksForColumnAsync(Guid columnId)
    {
        var list = await _tasks.ListByColumnAsync(columnId);
        return list.Select(t => new TaskDto
        {
            Id = t.Id,
            Name = t.Name,
            Priority = t.Priority,
            AssignedUserId = t.AssignedUserId,
            ColumnId = t.ColumnId
        }).ToList();
    }

    public async Task<TaskDto> UpdateTaskAsync(Guid taskId, UpdateTaskRequest req)
    {
        var task = await _tasks.GetByIdAsync(taskId)
                   ?? throw new KeyNotFoundException("Task not found.");

        // Apply changes
        if (req.Name is { } n) task.Name = n;
        if (req.Priority is { } p) task.Priority = p;
        if (req.AssignedUserId is { } uId)
        {
            // validate user exists
            var usr = await _users.GetByIdWithBoardsAsync(uId)
                      ?? throw new KeyNotFoundException("Assigned user not found.");
            task.AssignedUserId = uId;
        }

        // EF Core change-tracking picks up the modifications
        await _uow.SaveChangesAsync();

        return new TaskDto
        {
            Id = task.Id,
            Name = task.Name,
            Priority = task.Priority,
            AssignedUserId = task.AssignedUserId,
            ColumnId = task.ColumnId
        };
    }

    public async Task DeleteTaskAsync(Guid taskId)
    {
        var task = await _tasks.GetByIdAsync(taskId)
                   ?? throw new KeyNotFoundException("Task not found.");
        _tasks.Remove(task);
        await _uow.SaveChangesAsync();
    }
}