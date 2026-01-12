namespace TrelloClone.Server.Application.Interfaces;

public class RefreshTokenInfo
{
    public Guid UserId { get; set; }
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public string DeviceId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public interface IRefreshTokenService
{
    Task<string> GenerateRefreshTokenAsync(Guid userId, string deviceId = "");
    Task<RefreshTokenInfo?> ValidateRefreshTokenAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(string refreshToken);
    Task RevokeAllUserTokensAsync(Guid userId, string exceptToken = "");
    Task SetActiveSessionAsync(Guid userId, string sessionId);
    Task<string?> GetActiveSessionAsync(Guid userId);
}
