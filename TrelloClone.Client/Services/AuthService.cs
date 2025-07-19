using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Security.Claims;
using TrelloClone.Shared.DTOs;

namespace TrelloClone.Client.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<UserDto?> GetCurrentUserAsync();
    Task<bool> ValidateTokenAsync();
}

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly AuthStateProvider _authStateProvider;

    public AuthService(HttpClient httpClient, AuthenticationStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _authStateProvider = (AuthStateProvider)authStateProvider;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);

        if (response.IsSuccessStatusCode)
        {
            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (authResponse?.Token != null)
            {
                await _authStateProvider.SetTokenAsync(authResponse.Token);
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
            if (authResponse?.Token != null)
            {
                await _authStateProvider.SetTokenAsync(authResponse.Token);
                return authResponse;
            }
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"Registration failed: {response.StatusCode} - {errorContent}");
    }

    public async Task LogoutAsync()
    {
        await _authStateProvider.RemoveTokenAsync();
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        return authState.User.Identity?.IsAuthenticated == true;
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

    public async Task<bool> ValidateTokenAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/auth/me");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}