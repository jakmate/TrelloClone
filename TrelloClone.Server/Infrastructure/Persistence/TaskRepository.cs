using Microsoft.EntityFrameworkCore;

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _ctx;
    public TaskRepository(AppDbContext ctx) => _ctx = ctx;

    public void Add(TaskItem task) =>
        _ctx.Tasks.Add(task);

    public async Task<TaskItem?> GetByIdAsync(Guid taskId) =>
        await _ctx.Tasks
                  .Include(t => t.AssignedUser)
                  .FirstOrDefaultAsync(t => t.Id == taskId);

    public async Task<List<TaskItem>> ListByColumnAsync(Guid columnId) =>
        await _ctx.Tasks
                  .Where(t => t.ColumnId == columnId)
                  .OrderBy(t => t.CreatedAt)
                  .ToListAsync();

    public void Remove(TaskItem task) =>
        _ctx.Tasks.Remove(task);
}