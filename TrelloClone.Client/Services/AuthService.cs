using System.Net.Http.Json;

using Microsoft.AspNetCore.Components.Authorization;

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
    Task<UserDto> UpdateUserAsync(UpdateUserRequest request);
    Task ChangePasswordAsync(ChangePasswordRequest request);
    Task DeleteAccountAsync();
    Task<AvailabilityResponse> CheckUsernameAvailabilityAsync(string username);
    Task<AvailabilityResponse> CheckEmailAvailabilityAsync(string email);
}

public partial class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly AuthStateProvider _authStateProvider;
    private readonly ILogger<AuthService> _logger;

    public AuthService(HttpClient httpClient, AuthenticationStateProvider authStateProvider, ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _authStateProvider = (AuthStateProvider)authStateProvider;
        _logger = logger;
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
        try
        {
            var response = await _httpClient.GetAsync("/api/auth/me");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();
                return result?.User;
            }
            return null;
        }
        catch
        {
            return null;
        }
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

    public async Task<UserDto> UpdateUserAsync(UpdateUserRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync("/api/auth/update-user", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            // Try to parse JSON error message
            try
            {
                var errorObj = System.Text.Json.JsonDocument.Parse(errorContent);
                if (errorObj.RootElement.TryGetProperty("message", out var message))
                {
                    throw new HttpRequestException(message.GetString());
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                Log.JsonParseError(_logger, ex);
            }

            throw new HttpRequestException(errorContent);
        }

        var updatedUser = await response.Content.ReadFromJsonAsync<UserDto>();
        return updatedUser ?? throw new InvalidOperationException("Failed to update user");
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync("/api/auth/change-password", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            try
            {
                var errorObj = System.Text.Json.JsonDocument.Parse(errorContent);
                if (errorObj.RootElement.TryGetProperty("message", out var message))
                {
                    throw new HttpRequestException(message.GetString());
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                Log.JsonParseError(_logger, ex);
            }

            throw new HttpRequestException(errorContent);
        }
    }

    public async Task DeleteAccountAsync()
    {
        var response = await _httpClient.DeleteAsync("api/auth/delete-account");
        response.EnsureSuccessStatusCode();
    }

    public async Task<AvailabilityResponse> CheckUsernameAvailabilityAsync(string username)
    {
        var response = await _httpClient.GetFromJsonAsync<AvailabilityResponse>(
            $"/api/auth/check-username/{Uri.EscapeDataString(username)}");
        return response ?? new AvailabilityResponse { IsAvailable = false };
    }

    public async Task<AvailabilityResponse> CheckEmailAvailabilityAsync(string email)
    {
        var response = await _httpClient.GetFromJsonAsync<AvailabilityResponse>(
            $"/api/auth/check-email/{Uri.EscapeDataString(email)}");
        return response ?? new AvailabilityResponse { IsAvailable = false };
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to parse error response as JSON")]
        public static partial void JsonParseError(ILogger logger, Exception exception);
    }
}
