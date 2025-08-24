using Microsoft.EntityFrameworkCore;

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _ctx;
    public TaskRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task<TaskItem?> GetByIdAsync(Guid taskId) =>
    await _ctx.Tasks
              .Include(t => t.AssignedUsers)
              .Include(t => t.TaskAssignments)
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
            }
        }
    }

    public async Task AssignUsersToTaskAsync(Guid taskId, List<Guid> userIds)
    {
        // Remove existing assignments
        var existingAssignments = await _ctx.TaskAssignments
            .Where(ta => ta.TaskId == taskId)
            .ToListAsync();
        _ctx.TaskAssignments.RemoveRange(existingAssignments);

        // Add new assignments
        var newAssignments = userIds.Select(userId => new TaskAssignment
        {
            TaskId = taskId,
            UserId = userId
        }).ToList();

        _ctx.TaskAssignments.AddRange(newAssignments);
    }

    public void Add(TaskItem task) =>
        _ctx.Tasks.Add(task);

    public void Update(TaskItem task) =>
        _ctx.Tasks.Update(task);

    public void Remove(TaskItem task) =>
        _ctx.Tasks.Remove(task);
}