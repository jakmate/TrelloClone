using Microsoft.AspNetCore.SignalR.Client;

using TrelloClone.Shared.DTOs;

namespace TrelloClone.Client.Services
{
    public class NotificationHubClient
    {
        private readonly HubConnection _hubConnection;

        public event Action<BoardInvitationDto>? OnInvitationReceived;

        public NotificationHubClient(HubConnection hubConnection)
        {
            _hubConnection = hubConnection;
            RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            _hubConnection.On<BoardInvitationDto>("ReceiveInvitation", invitation =>
            {
                OnInvitationReceived?.Invoke(invitation);
            });
        }
    }
}
