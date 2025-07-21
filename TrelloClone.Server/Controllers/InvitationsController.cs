using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TrelloClone.Shared.DTOs;


namespace TrelloClone.Server.Controllers;
[ApiController]
[Route("api/invitations")]
[Authorize]
public class InvitationsController : ControllerBase
{
    private readonly InvitationService _invitationService;

    public InvitationsController(InvitationService invitationService)
    {
        _invitationService = invitationService;
    }

    [HttpPost]
    public async Task<IActionResult> SendInvitation(SendInvitationDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();
        await _invitationService.SendInvitation(dto.BoardId, userGuid, dto.Username, dto.Permission);
        return Ok();
    }

    [HttpPatch("{invitationId}/accept")]
    public async Task<IActionResult> AcceptInvitation(Guid invitationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();
        await _invitationService.AcceptInvitation(invitationId, userGuid);
        return Ok();
    }

    [HttpGet]
    public async Task<ActionResult<List<BoardInvitationDto>>> GetPendingInvitations([FromQuery] Guid userId)
    {
        var invitations = await _invitationService.GetPendingInvitations(userId);
        return Ok(invitations);
    }
}