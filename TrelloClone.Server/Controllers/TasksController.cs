using Microsoft.AspNetCore.Mvc;
using TrelloClone.Shared.DTOs;

[ApiController]
[Route("api/columns/{columnId:guid}/tasks")]
public class TasksController : ControllerBase
{
    private readonly TaskService _svc;
    public TasksController(TaskService svc) => _svc = svc;

    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create(
        Guid columnId,
        [FromBody] CreateTaskRequest req)
    {
        if (req.ColumnId != columnId) return BadRequest();
        var dto = await _svc.CreateTaskAsync(req);
        return CreatedAtAction(
            nameof(GetAll),
            new { columnId },
            dto
        );
    }

    [HttpGet]
    public async Task<ActionResult<List<TaskDto>>> GetAll(Guid columnId)
    {
        var list = await _svc.GetTasksForColumnAsync(columnId);
        return Ok(list);
    }

    [HttpPut("{taskId:guid}")]
    public async Task<ActionResult<TaskDto>> Update(
        Guid columnId,
        Guid taskId,
        [FromBody] UpdateTaskRequest req)
    {
        // (Optionally verify task belongs to column)
        var dto = await _svc.UpdateTaskAsync(taskId, req);
        return Ok(dto);
    }

    [HttpDelete("{taskId:guid}")]
    public async Task<IActionResult> Delete(Guid columnId, Guid taskId)
    {
        // (Optionally verify task belongs to column)
        await _svc.DeleteTaskAsync(taskId);
        return NoContent();
    }
}