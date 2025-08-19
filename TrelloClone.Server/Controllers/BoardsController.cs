using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TrelloClone.Shared.DTOs;

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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        try
        {
            var dto = await _boardService.CreateBoardAsync(req.Name, req.OwnerId);
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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        await _boardService.DeleteBoardAsync(boardId);
        return NoContent();
    }

    [HttpGet("{boardId:guid}")]
    public async Task<ActionResult<BoardDto>> Get(Guid boardId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        try { 
            var dto = await _boardService.GetBoardAsync(boardId);
            if (dto == null) return NotFound();
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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        try
        {
            var dto = await _boardService.GetAllBoardsAsync(userGuid);
            if (dto == null) return Ok(new BoardDto[0]); // Return empty array instead of NotFound
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
        return Guid.Parse(userIdString);
    }

    [HttpPost("from-template")]
    public async Task<ActionResult<BoardDto>> CreateBoardFromTemplate([FromBody] CreateBoardFromTemplateRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

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
}