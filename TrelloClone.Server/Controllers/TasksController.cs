using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TrelloClone.Shared.DTOs;

[ApiController]
[Route("api/columns/{columnId:guid}/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly TaskService _svc;
    public TasksController(TaskService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<List<TaskDto>>> GetAll(Guid columnId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        var list = await _svc.GetTasksForColumnAsync(columnId);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create(
    Guid columnId,
    [FromBody] CreateTaskRequest req)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        if (req.ColumnId != columnId) return BadRequest();
        var dto = await _svc.CreateTaskAsync(req);
        return CreatedAtAction(
            nameof(GetAll),
            new { columnId },
            dto
        );
    }

    [HttpPut("{taskId:guid}")]
    public async Task<ActionResult<TaskDto>> Update(
        Guid columnId,
        Guid taskId,
        [FromBody] UpdateTaskRequest req)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        var dto = await _svc.UpdateTaskAsync(taskId, req);
        return Ok(dto);
    }

    [HttpDelete("{taskId:guid}")]
    public async Task<IActionResult> Delete(Guid columnId, Guid taskId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        await _svc.DeleteTaskAsync(taskId);
        return NoContent();
    }

    [HttpPut("/api/tasks/{taskId:guid}/move")]
    public async Task<IActionResult> MoveTask(Guid taskId, [FromBody] MoveTaskRequest req)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        await _svc.MoveTaskAsync(taskId, req.ColumnId);
        return Ok();
    }
}