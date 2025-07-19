using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Net;

namespace TrelloClone.Client.Services
{
    public class AuthHttpMessageHandler : DelegatingHandler
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly AuthStateProvider _authStateProvider;

        public AuthHttpMessageHandler(IJSRuntime jsRuntime, AuthStateProvider authStateProvider)
        {
            _jsRuntime = jsRuntime;
            _authStateProvider = authStateProvider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var token = await _authStateProvider.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                Console.WriteLine($"Added token to request: {request.RequestUri}");
            }
            else
            {
                Console.WriteLine($"No token found for request: {request.RequestUri}");
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}