using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moq;

using TrelloClone.Server.Application.Interfaces;
using TrelloClone.Server.Controllers;
using TrelloClone.Shared.DTOs.Task;
using TrelloClone.Shared.DTOs.User;

using Xunit;

namespace TrelloClone.Server.Tests.Controllers;

public class TasksControllerTests
{
    private readonly Mock<ITaskService> _mockService;
    private readonly TasksController _controller;

    public TasksControllerTests()
    {
        _mockService = new Mock<ITaskService>();
        _controller = new TasksController(_mockService.Object);
    }

    private void SetupUser(string userId)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetAll_InvalidUserId_ReturnsUnauthorized()
    {
        // Arrange
        SetupUser("invalid");

        // Act
        var result = await _controller.GetAll(Guid.NewGuid());

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task GetAll_ValidRequest_ReturnsOk()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        var tasks = new List<TaskDto> { new TaskDto { Id = Guid.NewGuid() } };
        _mockService.Setup(x => x.GetTasksForColumnAsync(It.IsAny<Guid>())).ReturnsAsync(tasks);

        // Act
        var result = await _controller.GetAll(Guid.NewGuid());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(tasks, okResult.Value);
    }

    [Fact]
    public async Task GetAvailableUsers_InvalidUserId_ReturnsUnauthorized()
    {
        // Arrange
        SetupUser("invalid");

        // Act
        var result = await _controller.GetAvailableUsers(Guid.NewGuid());

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task GetAvailableUsers_ValidRequest_ReturnsOk()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        var users = new List<UserDto> { new UserDto { Id = Guid.NewGuid() } };
        _mockService.Setup(x => x.GetAvailableUsersForTaskAsync(It.IsAny<Guid>())).ReturnsAsync(users);

        // Act
        var result = await _controller.GetAvailableUsers(Guid.NewGuid());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(users, okResult.Value);
    }

    [Fact]
    public async Task Create_InvalidUserId_ReturnsUnauthorized()
    {
        // Arrange
        SetupUser("invalid");

        // Act
        var result = await _controller.Create(Guid.NewGuid(), new CreateTaskRequest());

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task Create_MismatchedColumnId_ReturnsBadRequest()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        var columnId = Guid.NewGuid();
        var req = new CreateTaskRequest { ColumnId = Guid.NewGuid() };

        // Act
        var result = await _controller.Create(columnId, req);

        // Assert
        Assert.IsType<BadRequestResult>(result.Result);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        var columnId = Guid.NewGuid();
        var req = new CreateTaskRequest { ColumnId = columnId };
        var dto = new TaskDto { Id = Guid.NewGuid() };
        _mockService.Setup(x => x.CreateTaskAsync(req)).ReturnsAsync(dto);

        // Act
        var result = await _controller.Create(columnId, req);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(TasksController.GetAll), createdResult.ActionName);
        Assert.Equal(dto, createdResult.Value);
    }

    [Fact]
    public async Task Update_InvalidUserId_ReturnsUnauthorized()
    {
        // Arrange
        SetupUser("invalid");

        // Act
        var result = await _controller.Update(Guid.NewGuid(), Guid.NewGuid(), new UpdateTaskRequest());

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task Update_ValidRequest_ReturnsOk()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        var dto = new TaskDto { Id = Guid.NewGuid() };
        _mockService.Setup(x => x.UpdateTaskAsync(It.IsAny<Guid>(), It.IsAny<UpdateTaskRequest>())).ReturnsAsync(dto);

        // Act
        var result = await _controller.Update(Guid.NewGuid(), Guid.NewGuid(), new UpdateTaskRequest());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(dto, okResult.Value);
    }

    [Fact]
    public async Task Delete_InvalidUserId_ReturnsUnauthorized()
    {
        // Arrange
        SetupUser("invalid");

        // Act
        var result = await _controller.Delete(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Delete_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());

        // Act
        var result = await _controller.Delete(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockService.Verify(x => x.DeleteTaskAsync(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task ReorderTasks_InvalidUserId_ReturnsUnauthorized()
    {
        // Arrange
        SetupUser("invalid");

        // Act
        var result = await _controller.ReorderTasks(new ReorderTasksRequest());

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task ReorderTasks_ValidRequest_ReturnsOk()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        var request = new ReorderTasksRequest { Tasks = new List<TaskPositionDto>() };

        // Act
        var result = await _controller.ReorderTasks(request);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockService.Verify(x => x.ReorderTasksAsync(request.Tasks), Times.Once);
    }
}
