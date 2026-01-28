using TrelloClone.Shared.DTOs.Auth;
using TrelloClone.Shared.DTOs.User;

namespace TrelloClone.Server.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest req);
    Task<AuthResponse> RegisterAsync(RegisterRequest req);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest req);
    Task LogoutAsync(string refreshToken);
    Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId);
    Task<bool> CheckUsernameExistsAsync(string username);
    Task<bool> CheckEmailExistsAsync(string email);
    Task<AuthResponse> UpdateUserAsync(Guid userId, UpdateUserRequest request);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task DeleteAccountAsync(Guid userId, DeleteAccountRequest request);
}
