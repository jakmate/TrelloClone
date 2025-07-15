using System.Net.Http.Json;
using TrelloClone.Client.Models;

namespace TrelloClone.Client.Services;

public interface IBoardService
{
    Task<List<BoardDto>> GetBoardsAsync();
    Task<BoardDto?> GetBoardAsync(Guid id);
    Task<BoardDto> CreateBoardAsync(CreateBoardRequest request);
}

public class BoardService : IBoardService
{
    private readonly HttpClient _httpClient;

    public BoardService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<BoardDto>> GetBoardsAsync()
    {
        // For now, return empty list
        return await _httpClient.GetFromJsonAsync<List<BoardDto>>("api/boards")
        ?? new List<BoardDto>();
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
}