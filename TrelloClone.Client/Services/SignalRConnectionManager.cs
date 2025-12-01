using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;

namespace TrelloClone.Client.Services
{
    public partial class SignalRConnectionManager
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

        private async void OnAuthenticationStateChanged(Task<AuthenticationState> task)
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
                Log.AuthStateError(_logger, ex);
            }
        }

        private async Task StartConnectionAsync()
        {
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await _hubConnection.StartAsync();
                    Log.SignalRStarted(_logger);
                }
                catch (Exception ex)
                {
                    Log.SignalRStartError(_logger, ex);
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
                    Log.SignalRStopped(_logger);
                }
                catch (Exception ex)
                {
                    Log.SignalRStopError(_logger, ex);
                }
            }
        }

        private static partial class Log
        {
            [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "SignalR connection started")]
            public static partial void SignalRStarted(ILogger logger);

            [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "SignalR connection stopped")]
            public static partial void SignalRStopped(ILogger logger);

            [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Error starting SignalR connection")]
            public static partial void SignalRStartError(ILogger logger, Exception exception);

            [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Error stopping SignalR connection")]
            public static partial void SignalRStopError(ILogger logger, Exception exception);

            [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Error handling authentication state change")]
            public static partial void AuthStateError(ILogger logger, Exception exception);
        }
    }
}
