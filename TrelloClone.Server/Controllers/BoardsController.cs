using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using TrelloClone.Server.Application.Services;
using TrelloClone.Shared.DTOs.Board;
using TrelloClone.Shared.Enums;

namespace TrelloClone.Server.Controllers;

[ApiController]
[Route("api/boards")]
[Authorize]
public class BoardsController : ControllerBase
{
    private readonly BoardService _boardService;

    public BoardsController(BoardService boardService)
        => _boardService = boardService;

    [HttpPost]
    public async Task<ActionResult<BoardDto>> Create([FromBody] CreateBoardRequest req)
    {
        var userGuid = GetCurrentUserId();

        try
        {
            var dto = await _boardService.CreateBoardAsync(req.Name, userGuid);
            return CreatedAtAction(nameof(Get), new { boardId = dto.Id }, dto);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{boardId:guid}")]
    public async Task<ActionResult<BoardDto>> Update(Guid boardId, [FromBody] UpdateBoardRequest req)
    {
        var userGuid = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(req.Name))
        {
            return BadRequest("Board name cannot be empty.");
        }

        try
        {
            var dto = await _boardService.UpdateBoardAsync(
                boardId,
                req.Name,
                userGuid
            );

            return Ok(dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);  // 403
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);  // 409
        }
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> ReorderBoards([FromBody] ReorderBoardsRequest request)
    {
        var userId = GetCurrentUserId();
        try
        {
            await _boardService.ReorderBoardsAsync(request.Boards, userId);
            return Ok();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    [HttpDelete("{boardId:guid}")]
    public async Task<IActionResult> Delete(Guid boardId)
    {
        var userId = GetCurrentUserId();

        try
        {
            await _boardService.DeleteBoardAsync(boardId, userId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    [HttpPost("{boardId:guid}/leave")]
    public async Task<IActionResult> LeaveBoard(Guid boardId)
    {
        var userId = GetCurrentUserId();

        try
        {
            await _boardService.LeaveBoardAsync(boardId, userId);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{boardId:guid}")]
    public async Task<ActionResult<BoardDto>> Get(Guid boardId)
    {
        try
        {
            var dto = await _boardService.GetBoardAsync(boardId);
            if (dto == null)
            {
                return NotFound();
            }

            return Ok(dto);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<BoardDto[]>> GetBoards()
    {
        var userGuid = GetCurrentUserId();

        try
        {
            var dto = await _boardService.GetAllBoardsAsync(userGuid);
            if (dto == null)
            {
                return Ok(Array.Empty<BoardDto>()); // Return empty array instead of NotFound
            }

            return Ok(dto);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{boardId}/permission")]
    public async Task<ActionResult<PermissionLevel>> GetUserPermission(Guid boardId)
    {
        var userId = GetCurrentUserId();
        var permission = await _boardService.GetUserPermissionAsync(boardId, userId);
        return Ok(permission);
    }

    private Guid GetCurrentUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            throw new UnauthorizedAccessException("User is not authenticated or invalid ID.");
        }
        return userId;
    }

    [HttpPost("from-template")]
    public async Task<ActionResult<BoardDto>> CreateBoardFromTemplate([FromBody] CreateBoardFromTemplateRequest request)
    {
        var userGuid = GetCurrentUserId();

        request.OwnerId = userGuid;

        try
        {
            var dto = await _boardService.CreateBoardFromTemplateAsync(request);
            return CreatedAtAction(nameof(Get), new { boardId = dto.Id }, dto);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{boardId:guid}/is-owner")]
    public async Task<ActionResult<bool>> IsOwner(Guid boardId)
    {
        var userId = GetCurrentUserId();
        var isOwner = await _boardService.IsOwnerAsync(boardId, userId);
        return Ok(isOwner);
    }
}
