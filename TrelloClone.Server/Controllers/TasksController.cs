using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using TrelloClone.Server.Application.Interfaces;
using TrelloClone.Shared.DTOs.Task;
using TrelloClone.Shared.DTOs.User;

namespace TrelloClone.Server.Controllers;

[ApiController]
[Route("api/columns/{columnId:guid}/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _tasksSvc;
    public TasksController(ITaskService tasksSvc) => _tasksSvc = tasksSvc;

    [HttpGet]
    public async Task<ActionResult<List<TaskDto>>> GetAll(Guid columnId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out _))
        {
            return Unauthorized();
        }

        var list = await _tasksSvc.GetTasksForColumnAsync(columnId);
        return Ok(list);
    }

    [HttpGet("available-users")]
    public async Task<ActionResult<List<UserDto>>> GetAvailableUsers(Guid columnId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out _))
        {
            return Unauthorized();
        }

        var users = await _tasksSvc.GetAvailableUsersForTaskAsync(columnId);
        return Ok(users);
    }

    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create(
        Guid columnId,
        [FromBody] CreateTaskRequest req)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out _))
        {
            return Unauthorized();
        }

        if (req.ColumnId != columnId)
        {
            return BadRequest();
        }

        var dto = await _tasksSvc.CreateTaskAsync(req);
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
        if (!Guid.TryParse(userId, out _))
        {
            return Unauthorized();
        }

        var dto = await _tasksSvc.UpdateTaskAsync(taskId, req);
        return Ok(dto);
    }

    [HttpDelete("{taskId:guid}")]
    public async Task<IActionResult> Delete(Guid columnId, Guid taskId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out _))
        {
            return Unauthorized();
        }

        await _tasksSvc.DeleteTaskAsync(taskId);
        return NoContent();
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> ReorderTasks([FromBody] ReorderTasksRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out _))
        {
            return Unauthorized();
        }

        await _tasksSvc.ReorderTasksAsync(request.Tasks);
        return Ok();
    }
}
