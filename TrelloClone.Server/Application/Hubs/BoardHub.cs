using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace TrelloClone.Server.Application.Hubs
{
    public class BoardHub : Hub
    {
        public async Task JoinBoardGroup(Guid boardId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, boardId.ToString());
        }
    }
}