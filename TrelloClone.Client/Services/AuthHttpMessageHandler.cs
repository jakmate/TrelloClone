using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

using TrelloClone.Shared.DTOs.Auth;
using TrelloClone.Shared.DTOs.User;
using TrelloClone.Shared.Enums;

namespace TrelloClone.Client.Services;

public partial class AuthHttpMessageHandler : DelegatingHandler
{
    private readonly AuthStateProvider _authStateProvider;
    private readonly IJSRuntime _jsRuntime;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthHttpMessageHandler> _logger;

    public AuthHttpMessageHandler(
        AuthStateProvider authStateProvider,
        IJSRuntime jsRuntime,
        IConfiguration configuration,
        ILogger<AuthHttpMessageHandler> logger)
    {
        _authStateProvider = authStateProvider;
        _jsRuntime = jsRuntime;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _authStateProvider.GetTokenAsync();

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // If 401 and not already a refresh request, try to refresh
        if (response.StatusCode == HttpStatusCode.Unauthorized &&
            !request.RequestUri!.AbsolutePath.Contains("/auth/refresh"))
        {
            var refreshToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "refreshToken");
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var refreshed = await TryRefreshTokenAsync(refreshToken);
                if (refreshed)
                {
                    // Retry original request
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", await _authStateProvider.GetTokenAsync());
                    response = await base.SendAsync(request, cancellationToken);
                }
            }
        }

        return response;
    }

    private async Task<bool> TryRefreshTokenAsync(string refreshToken)
    {
        try
        {
            var apiBaseUrl = _configuration["ApiBaseUrl"];
            if (string.IsNullOrEmpty(apiBaseUrl))
            {
                Log.ApiBaseUrlNotConfigured(_logger);
                return false;
            }

            using var httpClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl!) };

            var refreshResponse = await httpClient.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest { RefreshToken = refreshToken });

            if (refreshResponse.IsSuccessStatusCode)
            {
                var authResponse = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();
                if (authResponse?.Token != null)
                {
                    await _authStateProvider.SetTokenAsync(authResponse.Token);
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken", authResponse.RefreshToken);
                    return true;
                }
                else
                {
                    Log.RefreshTokenResponseInvalid(_logger);
                }
            }
            else
            {
                var errorContent = await refreshResponse.Content.ReadAsStringAsync();
                Log.RefreshTokenFailed(_logger, refreshResponse.StatusCode, errorContent);
            }
        }
        catch (HttpRequestException httpEx)
        {
            Log.HttpErrorDuringRefresh(_logger, httpEx);
        }
        catch (JsonException jsonEx)
        {
            Log.FailedToParseRefreshTokenResponse(_logger, jsonEx);
        }
        catch (UriFormatException uriEx)
        {
            Log.InvalidApiBaseUrlFormat(_logger, uriEx);
        }
        catch (Exception ex)
        {
            Log.UnexpectedErrorDuringRefresh(_logger, ex);
        }

        // Clear tokens on failure
        await _authStateProvider.RemoveTokenAsync();
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "refreshToken");
        return false;
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Error,
            Message = "ApiBaseUrl is not configured.")]
        public static partial void ApiBaseUrlNotConfigured(ILogger logger);

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Warning,
            Message = "Refresh token response did not contain a valid token.")]
        public static partial void RefreshTokenResponseInvalid(ILogger logger);

        [LoggerMessage(
            EventId = 3,
            Level = LogLevel.Warning,
            Message = "Refresh token failed with status {StatusCode}: {ErrorContent}")]
        public static partial void RefreshTokenFailed(ILogger logger, HttpStatusCode statusCode, string errorContent);

        [LoggerMessage(
            EventId = 4,
            Level = LogLevel.Error,
            Message = "HTTP error during token refresh.")]
        public static partial void HttpErrorDuringRefresh(ILogger logger, Exception exception);

        [LoggerMessage(
            EventId = 5,
            Level = LogLevel.Error,
            Message = "Failed to parse refresh token response.")]
        public static partial void FailedToParseRefreshTokenResponse(ILogger logger, Exception exception);

        [LoggerMessage(
            EventId = 6,
            Level = LogLevel.Error,
            Message = "Invalid ApiBaseUrl format.")]
        public static partial void InvalidApiBaseUrlFormat(ILogger logger, Exception exception);

        [LoggerMessage(
            EventId = 7,
            Level = LogLevel.Error,
            Message = "Unexpected error during token refresh.")]
        public static partial void UnexpectedErrorDuringRefresh(ILogger logger, Exception exception);
    }
}
