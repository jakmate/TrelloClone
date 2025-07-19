using System.Net.Http.Json;
using TrelloClone.Shared.DTOs;

namespace TrelloClone.Client.Services;

public class TaskService
{
    private readonly HttpClient _httpClient;

    public TaskService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<TaskDto>> GetTasksForColumnAsync(Guid columnId)
    {
        var response = await _httpClient.GetFromJsonAsync<List<TaskDto>>($"api/columns/{columnId}/tasks");
        return response ?? new List<TaskDto>();
    }

    public async Task<TaskDto> CreateTaskAsync(CreateTaskRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/columns/{request.ColumnId}/tasks", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TaskDto>()
               ?? throw new InvalidOperationException("Failed to create task");
    }

    public async Task<TaskDto> UpdateTaskAsync(Guid columnId, Guid taskId, UpdateTaskRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/columns/{columnId}/tasks/{taskId}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TaskDto>()
               ?? throw new InvalidOperationException("Failed to update task");
    }

    public async Task DeleteTaskAsync(Guid columnId, Guid taskId)
    {
        var response = await _httpClient.DeleteAsync($"api/columns/{columnId}/tasks/{taskId}");
        response.EnsureSuccessStatusCode();
    }
}