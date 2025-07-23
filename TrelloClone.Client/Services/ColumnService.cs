using System.Net.Http.Json;
using TrelloClone.Shared.DTOs;

namespace TrelloClone.Client.Services;

public class ColumnService
{
    private readonly HttpClient _httpClient;

    public ColumnService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ColumnDto>> GetColumnsForBoardAsync(Guid boardId)
    {
        var response = await _httpClient.GetFromJsonAsync<List<ColumnDto>>($"api/boards/{boardId}/columns");
        return response ?? new List<ColumnDto>();
    }

    public async Task<ColumnDto> CreateColumnAsync(CreateColumnRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/boards/{request.BoardId}/columns", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ColumnDto>()
               ?? throw new InvalidOperationException("Failed to create column");
    }

    public async Task<ColumnDto> UpdateColumnAsync(Guid boardId, Guid columnId, UpdateColumnRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/boards/{boardId}/columns/{columnId}", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Update failed: {response.StatusCode} - {error}");
            }

            return await response.Content.ReadFromJsonAsync<ColumnDto>()
                   ?? throw new InvalidOperationException("Failed to update column");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UpdateColumnAsync error: {ex}");
            throw;
        }
    }

    public async Task DeleteColumnAsync(Guid boardId, Guid columnId)
    {
        var response = await _httpClient.DeleteAsync($"api/boards/{boardId}/columns/{columnId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateColumnPositionAsync(Guid boardId, Guid columnId, int position)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/boards/{boardId}/columns/{columnId}/position", new { Position = position });
        response.EnsureSuccessStatusCode();
    }
}