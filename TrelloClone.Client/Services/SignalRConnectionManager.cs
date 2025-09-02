using Microsoft.AspNetCore.SignalR.Client;

namespace TrelloClone.Client.Services
{
    public class SignalRConnectionManager
    {
        private readonly HubConnection _hubConnection;
        private readonly AuthStateProvider _authStateProvider;
        private readonly ILogger<SignalRConnectionManager> _logger;

        public SignalRConnectionManager(
            HubConnection hubConnection,
            AuthStateProvider authStateProvider,
            ILogger<SignalRConnectionManager> logger)
        {
            _hubConnection = hubConnection;
            _authStateProvider = authStateProvider;
            _logger = logger;

            // Subscribe to authentication state changes
            _authStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
        }

        private async void OnAuthenticationStateChanged(Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState> task)
        {
            try
            {
                var state = await task;
                if (state.User.Identity?.IsAuthenticated == true)
                {
                    await StartConnectionAsync();
                }
                else
                {
                    await StopConnectionAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling authentication state change");
            }
        }

        private async Task StartConnectionAsync()
        {
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await _hubConnection.StartAsync();
                    _logger.LogInformation("SignalR connection started");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error starting SignalR connection");
                }
            }
        }

        private async Task StopConnectionAsync()
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                try
                {
                    await _hubConnection.StopAsync();
                    _logger.LogInformation("SignalR connection stopped");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping SignalR connection");
                }
            }
        }
    }
}