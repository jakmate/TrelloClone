using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using TrelloClone.Server.Application.Services;
using TrelloClone.Shared.DTOs.Column;

namespace TrelloClone.Server.Controllers;

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
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out _))
            {
                return Unauthorized();
            }

            var list = await _columnService.GetColumnsForBoardAsync(boardId);
            if (list == null)
            {
                return NotFound();
            }

            return Ok(list);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ColumnDto>> Create(
    Guid boardId,
    [FromBody] CreateColumnRequest req)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out _))
        {
            return Unauthorized();
        }

        if (req.BoardId != boardId)
        {
            return BadRequest();
        }

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
        if (!Guid.TryParse(userId, out _))
        {
            return Unauthorized();
        }

        var dto = await _columnService.UpdateColumnAsync(boardId, columnId, req);
        return Ok(dto);
    }

    [HttpDelete("{columnId:guid}")]
    public async Task<IActionResult> Delete(Guid columnId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out _))
        {
            return Unauthorized();
        }

        await _columnService.DeleteColumnAsync(columnId);
        return NoContent();
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> ReorderColumns(Guid boardId, [FromBody] ReorderColumnsRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out _))
        {
            return Unauthorized();
        }

        await _columnService.ReorderColumnsAsync(boardId, request.Columns);
        return Ok();
    }
}
