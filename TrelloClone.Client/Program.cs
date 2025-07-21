using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using TrelloClone.Client;
using TrelloClone.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<AuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<AuthStateProvider>());
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBoardService>(provider =>
{
    var httpClient = provider.GetRequiredService<HttpClient>();
    var authStateProvider = provider.GetRequiredService<AuthenticationStateProvider>();
    return new BoardService(httpClient, authStateProvider);
});
builder.Services.AddScoped<IInvitationService, InvitationService>();
builder.Services.AddScoped<ColumnService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<AuthHttpMessageHandler>();

builder.Services.AddScoped<HttpClient>(sp =>
{
    var handler = sp.GetRequiredService<AuthHttpMessageHandler>();
    handler.InnerHandler = new HttpClientHandler();
    
    var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri("http://localhost:5084")
    };
    
    return httpClient;
});

builder.Services.AddScoped(provider =>
{
    var authStateProvider = provider.GetRequiredService<AuthStateProvider>();
    return new HubConnectionBuilder()
        .WithUrl("http://localhost:5084/boardhub", options =>
        {
            options.AccessTokenProvider = async () =>
            {
                var token = await authStateProvider.GetTokenAsync();
                return token;
            };
        })
        .WithAutomaticReconnect()
        .Build();
});

builder.Services.AddScoped<NotificationHubClient>(provider =>
{
    var hubConnection = provider.GetRequiredService<HubConnection>();
    var logger = provider.GetRequiredService<ILogger<NotificationHubClient>>();
    return new NotificationHubClient(hubConnection, logger);
});

await builder.Build().RunAsync();
