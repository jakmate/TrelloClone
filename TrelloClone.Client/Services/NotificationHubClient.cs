using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TrelloClone.Shared.DTOs;

namespace TrelloClone.Client.Services
{
    public class NotificationHubClient
    {
        private readonly HubConnection _hubConnection;
        private readonly ILogger<NotificationHubClient> _logger;

        public event Action<BoardInvitationDto>? OnInvitationReceived;

        public NotificationHubClient(HubConnection hubConnection, ILogger<NotificationHubClient> logger)
        {
            _hubConnection = hubConnection;
            _logger = logger;
            RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            _hubConnection.On<BoardInvitationDto>("ReceiveInvitation", invitation =>
            {
                OnInvitationReceived?.Invoke(invitation);
            });
        }

        public async Task StartAsync()
        {
            try
            {
                await _hubConnection.StartAsync();
                _logger.LogInformation("SignalR connection started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting SignalR connection");
                throw;
            }
        }
    }
}