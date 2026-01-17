using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Moq;
using Moq.Protected;

using TrelloClone.Shared.DTOs.Task;
using TrelloClone.Shared.DTOs.User;
using TrelloClone.Shared.Enums;

using Xunit;

namespace TrelloClone.Client.Services.Tests;

public class TaskServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly TaskService _service;

    public TaskServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://localhost")
        };
        _service = new TaskService(_httpClient);
    }

    [Fact]
    public async Task GetTasksForColumnAsync_ValidColumnId_ReturnsTaskList()
    {
        // Arrange
        var columnId = Guid.NewGuid();
        var expectedTasks = new List<TaskDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Task 1",
                Priority = PriorityLevel.Medium,
                AssignedUserIds = new List<Guid> { Guid.NewGuid() },
                ColumnId = columnId,
                Position = 1
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Task 2",
                Priority = PriorityLevel.High,
                AssignedUserIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
                ColumnId = columnId,
                Position = 2
            }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expectedTasks)
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri.ToString().Contains($"api/columns/{columnId}/tasks")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _service.GetTasksForColumnAsync(columnId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTasks.Count, result.Count);
        Assert.Equal(expectedTasks[0].Name, result[0].Name);
        Assert.Equal(expectedTasks[0].Priority, result[0].Priority);
        Assert.Equal(expectedTasks[1].Name, result[1].Name);
    }

    [Fact]
    public async Task GetAvailableUsersForTaskAsync_ValidColumnId_ReturnsUserList()
    {
        // Arrange
        var columnId = Guid.NewGuid();
        var expectedUsers = new List<UserDto>
        {
            new() { Id = Guid.NewGuid(), UserName = "John Doe", Email = "john@example.com" },
            new() { Id = Guid.NewGuid(), UserName = "Jane Smith", Email = "jane@example.com" }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expectedUsers)
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri.ToString().Contains($"api/columns/{columnId}/tasks/available-users")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _service.GetAvailableUsersForTaskAsync(columnId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedUsers.Count, result.Count);
        Assert.Equal(expectedUsers[0].UserName, result[0].UserName);
        Assert.Equal(expectedUsers[1].Email, result[1].Email);
    }

    [Fact]
    public async Task CreateTaskAsync_ValidRequest_CreatesTaskSuccessfully()
    {
        // Arrange
        var columnId = Guid.NewGuid();
        var assignedUserId = Guid.NewGuid();
        var request = new CreateTaskRequest
        {
            ColumnId = columnId,
            Name = "New Task",
            Priority = PriorityLevel.Low,
            AssignedUserIds = new List<Guid> { assignedUserId }
        };

        var expectedTask = new TaskDto
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Priority = request.Priority,
            AssignedUserIds = request.AssignedUserIds,
            ColumnId = columnId,
            Position = 1
        };

        var response = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(expectedTask)
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri.ToString().Contains($"api/columns/{columnId}/tasks")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _service.CreateTaskAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTask.Id, result.Id);
        Assert.Equal(expectedTask.Name, result.Name);
        Assert.Equal(expectedTask.Priority, result.Priority);
        Assert.Single(result.AssignedUserIds);
        Assert.Equal(assignedUserId, result.AssignedUserIds[0]);
    }

    [Fact]
    public async Task UpdateTaskAsync_ValidRequest_UpdatesTaskSuccessfully()
    {
        // Arrange
        var columnId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var assignedUserId1 = Guid.NewGuid();
        var assignedUserId2 = Guid.NewGuid();

        var request = new UpdateTaskRequest
        {
            Name = "Updated Task Name",
            Priority = PriorityLevel.High,
            AssignedUserIds = new List<Guid> { assignedUserId1, assignedUserId2 }
        };

        var expectedTask = new TaskDto
        {
            Id = taskId,
            Name = request.Name,
            Priority = request.Priority,
            AssignedUserIds = request.AssignedUserIds,
            ColumnId = columnId,
            Position = 1
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expectedTask)
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Put &&
                req.RequestUri.ToString().Contains($"api/columns/{columnId}/tasks/{taskId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _service.UpdateTaskAsync(columnId, taskId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTask.Id, result.Id);
        Assert.Equal(expectedTask.Name, result.Name);
        Assert.Equal(expectedTask.Priority, result.Priority);
        Assert.Equal(2, result.AssignedUserIds.Count);
        Assert.Contains(assignedUserId1, result.AssignedUserIds);
        Assert.Contains(assignedUserId2, result.AssignedUserIds);
    }

    [Fact]
    public async Task DeleteTaskAsync_ValidIds_DeletesTaskSuccessfully()
    {
        // Arrange
        var columnId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var response = new HttpResponseMessage(HttpStatusCode.NoContent);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Delete &&
                req.RequestUri.ToString().Contains($"api/columns/{columnId}/tasks/{taskId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => _service.DeleteTaskAsync(columnId, taskId));
        Assert.Null(exception);
    }

    [Fact]
    public async Task ReorderTasksAsync_ValidPositions_ReturnsTrueOnSuccess()
    {
        // Arrange
        var columnId = Guid.NewGuid();
        var positions = new List<TaskPositionDto>
        {
            new() { Id = Guid.NewGuid(), ColumnId = columnId, Position = 1 },
            new() { Id = Guid.NewGuid(), ColumnId = columnId, Position = 2 }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Put &&
                req.RequestUri.ToString().Contains($"api/columns/{columnId}/tasks/reorder")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _service.ReorderTasksAsync(positions);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CreateTaskAsync_ServerError_ThrowsHttpRequestException()
    {
        // Arrange
        var columnId = Guid.NewGuid();
        var request = new CreateTaskRequest
        {
            ColumnId = columnId,
            Name = "Invalid Task",
            Priority = PriorityLevel.Medium
        };

        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri.ToString().Contains($"api/columns/{columnId}/tasks")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _service.CreateTaskAsync(request));
    }

    [Fact]
    public async Task GetTasksForColumnAsync_EmptyResponse_ReturnsEmptyList()
    {
        // Arrange
        var columnId = Guid.NewGuid();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<TaskDto>())
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri.ToString().Contains($"api/columns/{columnId}/tasks")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _service.GetTasksForColumnAsync(columnId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
