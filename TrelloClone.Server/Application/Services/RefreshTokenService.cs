using System.Security.Cryptography;

using Microsoft.Extensions.Caching.Distributed;

using TrelloClone.Server.Application.Interfaces;

namespace TrelloClone.Server.Application.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private static class CacheKeys
    {
        public const string RefreshToken = "refresh_token:{0}";
        public const string UserRefreshTokens = "user_refresh_tokens:{0}";
        public const string ActiveSession = "active_session:{0}";
    }

    private readonly IDistributedCache _cache;

    public RefreshTokenService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<string> GenerateRefreshTokenAsync(Guid userId, string deviceId = "")
    {
        var refreshToken = GenerateSecureToken();
        var tokenKey = $"refresh_token:{refreshToken}";
        var userTokensKey = $"user_refresh_tokens:{userId}";

        var refreshTokenInfo = new RefreshTokenInfo
        {
            UserId = userId,
            DeviceId = deviceId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        // Store the refresh token
        await _cache.SetStringAsync(
            tokenKey,
            System.Text.Json.JsonSerializer.Serialize(refreshTokenInfo),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = refreshTokenInfo.ExpiresAt
            }
        );

        // Add to user's token list
        var existingTokens = await GetRefreshTokensForUserAsync(userId);
        existingTokens.Add(refreshToken);
        await _cache.SetStringAsync(
            userTokensKey,
            System.Text.Json.JsonSerializer.Serialize(existingTokens),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
            }
        );

        return refreshToken;
    }

    public async Task<RefreshTokenInfo?> ValidateRefreshTokenAsync(string refreshToken)
    {
        var tokenKey = $"refresh_token:{refreshToken}";
        var tokenJson = await _cache.GetStringAsync(tokenKey);

        if (string.IsNullOrEmpty(tokenJson))
        {
            return null;
        }

        var tokenInfo = System.Text.Json.JsonSerializer.Deserialize<RefreshTokenInfo>(tokenJson);

        if (tokenInfo?.ExpiresAt <= DateTime.UtcNow)
        {
            await RevokeRefreshTokenAsync(refreshToken);
            return null;
        }

        return tokenInfo;
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var tokenKey = $"refresh_token:{refreshToken}";
        await _cache.RemoveAsync(tokenKey);
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, string exceptToken = "")
    {
        var userTokensKey = $"user_refresh_tokens:{userId}";
        var existingTokens = await GetRefreshTokensForUserAsync(userId);

        foreach (var token in existingTokens.Where(t => t != exceptToken))
        {
            await RevokeRefreshTokenAsync(token);
        }

        // Clear the user's token list and add only the exception token if any
        if (!string.IsNullOrEmpty(exceptToken))
        {
            await _cache.SetStringAsync(
                userTokensKey,
                System.Text.Json.JsonSerializer.Serialize(new List<string> { exceptToken }),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
                }
            );
        }
        else
        {
            await _cache.RemoveAsync(userTokensKey);
        }
    }

    private async Task<List<string>> GetRefreshTokensForUserAsync(Guid userId)
    {
        var userTokensKey = $"user_refresh_tokens:{userId}";
        var tokensJson = await _cache.GetStringAsync(userTokensKey);

        if (string.IsNullOrEmpty(tokensJson))
        {
            return new List<string>();
        }

        return System.Text.Json.JsonSerializer.Deserialize<List<string>>(tokensJson) ?? new List<string>();
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes);
    }

    public async Task SetActiveSessionAsync(Guid userId, string sessionId)
    {
        var key = $"active_session:{userId}";
        await _cache.SetStringAsync(key, sessionId, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
        });
    }

    public async Task<string?> GetActiveSessionAsync(Guid userId)
    {
        var key = $"active_session:{userId}";
        return await _cache.GetStringAsync(key);
    }
}
