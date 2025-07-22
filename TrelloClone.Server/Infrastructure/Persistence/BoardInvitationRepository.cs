using Microsoft.EntityFrameworkCore;
using TrelloClone.Shared.DTOs;

public class BoardInvitationRepository : IBoardInvitationRepository
{
    private readonly AppDbContext _ctx;

    public BoardInvitationRepository(AppDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<BoardInvitation?> GetByIdAsync(Guid id) =>
        await _ctx.BoardInvitation.FirstOrDefaultAsync(i => i.Id == id);

    public void Add(BoardInvitation invitation) =>
        _ctx.BoardInvitation.Add(invitation);

    public async Task<List<BoardInvitationDto>> GetPendingInvitations(Guid userId)
    {
        return await _ctx.BoardInvitation
            .Where(i => i.InvitedUserId == userId && i.Status == InvitationStatus.Pending)
            .Include(i => i.Board)
            .Include(i => i.InviterUser)
            .Select(i => new BoardInvitationDto
            {
                Id = i.Id,
                BoardId = i.BoardId,
                BoardName = i.Board.Name,
                InviterName = i.InviterUser.UserName,
                SentAt = i.SentAt
            })
            .ToListAsync();
    }

    public async Task<BoardInvitation?> GetPendingInvitationAsync(Guid boardId, Guid userId)
    {
        return await _ctx.BoardInvitation
            .FirstOrDefaultAsync(i => i.BoardId == boardId &&
                                     i.InvitedUserId == userId &&
                                     i.Status == InvitationStatus.Pending);
    }
}