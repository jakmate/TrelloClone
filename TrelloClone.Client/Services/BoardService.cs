using System.Net;
using System.Net.Http.Json;
using TrelloClone.Shared.DTOs;

namespace TrelloClone.Client.Services;

public interface IBoardService
{
    Task<List<BoardDto>> GetBoardsAsync(Guid ownerId);
    Task<BoardDto?> GetBoardAsync(Guid id);
    Task<BoardDto> CreateBoardAsync(CreateBoardRequest request);
    Task<BoardDto> UpdateBoardAsync(Guid id, UpdateBoardRequest request);
    Task<bool> DeleteBoardAsync(Guid id);
}

public class BoardService : IBoardService
{
    private readonly HttpClient _httpClient;

    public BoardService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<BoardDto>> GetBoardsAsync(Guid ownerId)
    {
        return await _httpClient.GetFromJsonAsync<List<BoardDto>>($"api/boards?ownerId={ownerId}") ?? new List<BoardDto>(); ;
    }

    public async Task<BoardDto?> GetBoardAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<BoardDto>($"api/boards/{id}");
    }

    public async Task<BoardDto> CreateBoardAsync(CreateBoardRequest request)
    {   
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

        // Return true for successful deletion (204 No Content)
        if (response.StatusCode == HttpStatusCode.NoContent)
            return true;

        // Throw exception for error cases
        var error = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException($"Delete failed: {error}");
    }
}