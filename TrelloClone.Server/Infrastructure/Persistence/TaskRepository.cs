using Microsoft.EntityFrameworkCore;

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _ctx;
    public TaskRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task<TaskItem?> GetByIdAsync(Guid taskId) =>
    await _ctx.Tasks
              .Include(t => t.AssignedUsers)
              .Include(t => t.TaskAssignments)
              .Include(t => t.Column)
              .FirstOrDefaultAsync(t => t.Id == taskId);

    public async Task<List<TaskItem>> ListByColumnAsync(Guid columnId) =>
        await _ctx.Tasks
                  .Where(t => t.ColumnId == columnId)
                  .Include(t => t.AssignedUsers)
                  .OrderBy(t => t.Position)
                  .ThenBy(t => t.CreatedAt)
                  .ToListAsync();

    public async Task UpdatePositionsAsync(List<TaskPositionDto> positions)
    {
        var taskIds = positions.Select(p => p.Id).ToList();
        var tasks = await _ctx.Tasks.Where(t => taskIds.Contains(t.Id)).ToListAsync();

        foreach (var pos in positions)
        {
            var task = tasks.FirstOrDefault(t => t.Id == pos.Id);
            if (task != null)
            {
                task.Position = pos.Position;

                if (pos.ColumnId.HasValue && task.ColumnId != pos.ColumnId.Value)
                {
                    task.ColumnId = pos.ColumnId.Value;
                }
            }
        }
    }

    public async Task AssignUsersToTaskAsync(Guid taskId, List<Guid> userIds)
    {
        // load the task with its AssignedUsers navigation
        var task = await _ctx.Tasks
                             .Include(t => t.AssignedUsers)
                             .FirstOrDefaultAsync(t => t.Id == taskId)
                   ?? throw new KeyNotFoundException($"Task {taskId} not found");

        // Clear existing navigation items
        task.AssignedUsers.Clear();

        if (userIds != null && userIds.Any())
        {
            // load users and add them to the navigation collection
            var users = await _ctx.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();

            foreach (var u in users)
                task.AssignedUsers.Add(u);
        }
    }

    public void Add(TaskItem task) =>
        _ctx.Tasks.Add(task);

    public void Update(TaskItem task) =>
        _ctx.Tasks.Update(task);

    public void Remove(TaskItem task) =>
        _ctx.Tasks.Remove(task);
}