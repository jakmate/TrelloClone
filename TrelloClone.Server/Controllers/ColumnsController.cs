using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/boards/{boardId:guid}/columns")]
public class ColumnsController : ControllerBase
{
    private readonly ColumnService _svc;
    public ColumnsController(ColumnService svc) => _svc = svc;

    [HttpPost]
    public async Task<ActionResult<ColumnDto>> Create(
        Guid boardId,
        [FromBody] CreateColumnRequest req)
    {
        // ensure route & body match
        if (req.BoardId != boardId) return BadRequest();
        var dto = await _svc.CreateColumnAsync(req);
        return CreatedAtAction(
            nameof(GetAll),
            new { boardId },
            dto
        );
    }

    [HttpGet]
    public async Task<ActionResult<List<ColumnDto>>> GetAll(Guid boardId)
    {
        var list = await _svc.GetColumnsForBoardAsync(boardId);
        return Ok(list);
    }

    [HttpDelete("{columnId:guid}")]
    public async Task<IActionResult> Delete(Guid boardId, Guid columnId)
    {
        // optionally verify column belongs to board
        await _svc.DeleteColumnAsync(columnId);
        return NoContent();
    }
}
