using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using TrelloClone.Server.Application.Interfaces;
using TrelloClone.Server.Application.Services;
using TrelloClone.Shared.DTOs;

namespace TrelloClone.Server.Controllers;

[ApiController]
[Route("api/invitations")]
[Authorize]
public class InvitationsController : ControllerBase
{
    private readonly IInvitationService _invitationSvc;

    public InvitationsController(IInvitationService invitationSvc) => _invitationSvc = invitationSvc;

    [HttpPost]
    public async Task<IActionResult> SendInvitation(SendInvitationDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(dto.Username))
            {
                return BadRequest("Username cannot be empty.");
            }

            await _invitationSvc.SendInvitation(dto.BoardId, userGuid, dto.Username, dto.Permission);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest($"Error: {ex.Message}");
        }
    }

    [HttpPatch("{invitationId}/accept")]
    public async Task<IActionResult> AcceptInvitation(Guid invitationId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            await _invitationSvc.AcceptInvitation(invitationId, userGuid);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest($"Error: {ex.Message}");
        }
    }

    [HttpPatch("{invitationId}/decline")]
    public async Task<IActionResult> DeclineInvitation(Guid invitationId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            await _invitationSvc.DeclineInvitation(invitationId, userGuid);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest($"Error: {ex.Message}");
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<BoardInvitationDto>>> GetPendingInvitations([FromQuery] Guid userId)
    {
        var invitations = await _invitationSvc.GetPendingInvitations(userId);
        return Ok(invitations);
    }
}
