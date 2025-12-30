namespace TrelloClone.Client.Services
{
    public class AuthHttpMessageHandler : DelegatingHandler
    {
        private readonly AuthStateProvider _authStateProvider;

        public AuthHttpMessageHandler(AuthStateProvider authStateProvider)
        {
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
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
