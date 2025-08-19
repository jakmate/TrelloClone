using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using TrelloClone.Shared.DTOs;

namespace TrelloClone.Client.Services;

public interface IBoardService
{
    Task<List<BoardDto>> GetBoardsAsync();
    Task<BoardDto?> GetBoardAsync(Guid id);
    Task<BoardDto> CreateBoardAsync(CreateBoardRequest request);
    Task<BoardDto> UpdateBoardAsync(Guid id, UpdateBoardRequest request);
    Task<bool> DeleteBoardAsync(Guid id);
    
    Task<PermissionLevel> GetUserPermissionAsync(Guid boardId);
    Task<bool> CanEditAsync(Guid boardId);
    Task<bool> CanInviteAsync(Guid boardId);
    Task<bool> ReorderBoardsAsync(List<BoardPositionDto> positions);
    Task<BoardDto> CreateBoardFromTemplateAsync(CreateBoardFromTemplateRequest request);
}

public class BoardService : IBoardService
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationStateProvider _authStateProvider;

    public BoardService(HttpClient httpClient, AuthenticationStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _authStateProvider = authStateProvider;
    }

    public async Task<List<BoardDto>> GetBoardsAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return new List<BoardDto>();

        var response = await _httpClient.GetFromJsonAsync<BoardDto[]>($"api/boards?ownerId={userId}");
        return response?.ToList() ?? new List<BoardDto>();
    }

    public async Task<BoardDto?> GetBoardAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<BoardDto>($"api/boards/{id}");
    }

    public async Task<BoardDto> CreateBoardAsync(CreateBoardRequest request)
    {
        // Get current user ID for the request
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            throw new UnauthorizedAccessException("User not authenticated");

        // Set the OwnerId if not already set
        request.OwnerId = userGuid;

        var response = await _httpClient.PostAsJsonAsync("api/boards", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BoardDto>() ?? throw new InvalidOperationException("Failed to create board");
    }

    public async Task<BoardDto> UpdateBoardAsync(Guid id, UpdateBoardRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/boards/{id}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BoardDto>() ?? throw new InvalidOperationException("Failed to update board");
    }

    public async Task<bool> DeleteBoardAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/boards/{id}");
        if (response.StatusCode == HttpStatusCode.NoContent)
            return true;
        var error = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException($"Delete failed: {error}");
    }

    public async Task<PermissionLevel> GetUserPermissionAsync(Guid boardId)
    {
        return await _httpClient.GetFromJsonAsync<PermissionLevel>($"api/boards/{boardId}/permission");
    }

    public async Task<bool> CanEditAsync(Guid boardId)
    {
        var permission = await GetUserPermissionAsync(boardId);
        return permission >= PermissionLevel.Editor;
    }

    public async Task<bool> CanInviteAsync(Guid boardId)
    {
        var permission = await GetUserPermissionAsync(boardId);
        return permission == PermissionLevel.Admin;
    }

    public async Task<bool> ReorderBoardsAsync(List<BoardPositionDto> positions)
    {
        var request = new ReorderBoardsRequest { Boards = positions };
        var response = await _httpClient.PutAsJsonAsync("api/boards/reorder", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<BoardDto> CreateBoardFromTemplateAsync(CreateBoardFromTemplateRequest request)
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            throw new UnauthorizedAccessException("User not authenticated");

        request.OwnerId = userGuid;

        var response = await _httpClient.PostAsJsonAsync("api/boards/from-template", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BoardDto>() ?? throw new Exception("Failed to create board from template");
    }
}