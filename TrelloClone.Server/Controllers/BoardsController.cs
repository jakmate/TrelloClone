using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/boards")]
public class BoardsController : ControllerBase
{
    private readonly BoardService _boardService;

    public BoardsController(BoardService boardService)
        => _boardService = boardService;

    [HttpPost]
    public async Task<ActionResult<BoardDto>> Create([FromBody] CreateBoardRequest req)
    {
        try
        {
            var dto = await _boardService.CreateBoardAsync(req.Name, req.OwnerId);
            return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BoardDto>> Get(Guid id)
    {
        try { 
            var dto = await _boardService.GetBoardAsync(id);
            if (dto == null) return NotFound();
            return Ok(dto);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<BoardDto>> GetBoards()
    {
        // For now, return empty list
        return Ok(new List<BoardDto>());
    }
}