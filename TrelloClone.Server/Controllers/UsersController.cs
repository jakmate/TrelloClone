using Microsoft.AspNetCore.Mvc;
using TrelloClone.Shared.DTOs;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserService _userSvc;
    public UsersController(UserService userSvc) => _userSvc = userSvc;

    [HttpPost("{userId:guid}/boards/{boardId:guid}")]
    public async Task<IActionResult> AddToBoard(Guid userId, Guid boardId)
    {
        await _userSvc.AddUserToBoardAsync(boardId, userId);
        return NoContent();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> Get(Guid id)
    {
        var dto = await _userSvc.GetUserAsync(id);
        if (dto == null) return NotFound();
        return Ok(dto);
    }
}
