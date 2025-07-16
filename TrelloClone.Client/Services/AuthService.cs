using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using TrelloClone.Client.Models;

namespace TrelloClone.Client.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<UserDto?> GetCurrentUserAsync();
}

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly AuthStateProvider _authStateProvider;

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime, AuthenticationStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _authStateProvider = (AuthStateProvider)authStateProvider;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);

        if (response.IsSuccessStatusCode)
        {
            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (authResponse != null)
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", authResponse.Token);
                _authStateProvider.MarkUserAsAuthenticated(authResponse.User);
                return authResponse;
            }
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        throw new UnauthorizedAccessException($"Login failed: {response.StatusCode} - {errorContent}");
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);

        if (response.IsSuccessStatusCode)
        {
            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (authResponse != null)
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", authResponse.Token);
                _authStateProvider.MarkUserAsAuthenticated(authResponse.User);
                return authResponse;
            }
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"Registration failed: {response.StatusCode} - {errorContent}");
    }

    public async Task LogoutAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
        _authStateProvider.MarkUserAsLoggedOut();
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
        return !string.IsNullOrEmpty(token);
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated == true)
        {
            var userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = authState.User.FindFirst(ClaimTypes.Name)?.Value;
            var email = authState.User.FindFirst(ClaimTypes.Email)?.Value;

            if (Guid.TryParse(userId, out var id))
            {
                return new UserDto
                {
                    Id = id,
                    UserName = userName ?? "",
                    Email = email ?? ""
                };
            }
        }
        return null;
    }
}