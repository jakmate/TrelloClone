using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TrelloClone.Shared.DTOs;

[ApiController]
[Route("api/boards/{boardId:guid}/columns")]
[Authorize]
public class ColumnsController : ControllerBase
{
    private readonly ColumnService _columnService;
    public ColumnsController(ColumnService columnService)
        => _columnService = columnService;

    [HttpGet]
    public async Task<ActionResult<List<ColumnDto>>> GetAll(Guid boardId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        var list = await _columnService.GetColumnsForBoardAsync(boardId);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<ColumnDto>> Create(
    Guid boardId,
    [FromBody] CreateColumnRequest req)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        if (req.BoardId != boardId) return BadRequest();
        var dto = await _columnService.CreateColumnAsync(req);
        return CreatedAtAction(
            nameof(GetAll),
            new { boardId },
            dto
        );
    }

    [HttpPut("{columnId:guid}")]
    public async Task<ActionResult<ColumnDto>> Update(
        Guid boardId,
        Guid columnId,
        [FromBody] UpdateColumnRequest req)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        var dto = await _columnService.UpdateColumnAsync(boardId, columnId, req);
        return Ok(dto);
    }

    [HttpDelete("{columnId:guid}")]
    public async Task<IActionResult> Delete(Guid columnId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        await _columnService.DeleteColumnAsync(columnId);
        return NoContent();
    }

    [HttpPut("{columnId:guid}/position")]
    public async Task<IActionResult> UpdatePosition(Guid boardId, Guid columnId, [FromBody] UpdateColumnPositionRequest req)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        await _columnService.UpdateColumnPositionAsync(columnId, req.Position);
        return Ok();
    }
}
