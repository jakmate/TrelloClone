using Microsoft.EntityFrameworkCore;

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _ctx;
    public TaskRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task<TaskItem?> GetByIdAsync(Guid taskId) =>
        await _ctx.Tasks
                  .Include(t => t.AssignedUser)
                  .FirstOrDefaultAsync(t => t.Id == taskId);

    public async Task<List<TaskItem>> ListByColumnAsync(Guid columnId) =>
        await _ctx.Tasks
                  .Where(t => t.ColumnId == columnId)
                  .OrderBy(t => t.Position)
                  .ThenBy(t => t.CreatedAt)
                  .ToListAsync();

    public async Task UpdatePositionsAsync(List<TaskPositionDto> positions)
    {
        foreach (var pos in positions)
        {
            var task = await _ctx.Tasks.FindAsync(pos.Id);
            if (task != null)
            {
                task.Position = pos.Position;
                if (pos.ColumnId.HasValue)
                    task.ColumnId = pos.ColumnId.Value;
            }
        }
    }

    public void Add(TaskItem task) =>
        _ctx.Tasks.Add(task);

    public void Update(TaskItem task) =>
        _ctx.Tasks.Update(task);

    public void Remove(TaskItem task) =>
        _ctx.Tasks.Remove(task);
}