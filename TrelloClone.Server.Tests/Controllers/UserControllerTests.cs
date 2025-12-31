using Microsoft.AspNetCore.Mvc;

using Moq;

using TrelloClone.Server.Application.Interfaces;
using TrelloClone.Server.Controllers;
using TrelloClone.Shared.DTOs;

namespace TrelloClone.Server.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _controller = new UsersController(_mockUserService.Object);
    }

    [Fact]
    public async Task AddToBoard_CallsServiceWithCorrectParameters_ReturnsNoContent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var boardId = Guid.NewGuid();

        _mockUserService
            .Setup(s => s.AddUserToBoardAsync(boardId, userId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.AddToBoard(userId, boardId);

        // Assert
        _mockUserService.Verify(s => s.AddUserToBoardAsync(boardId, userId), Times.Once);
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Get_UserExists_ReturnsOkWithUserDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userDto = new UserDto { Id = userId };

        _mockUserService
            .Setup(s => s.GetUserAsync(userId))
            .ReturnsAsync(userDto);

        // Act
        var result = await _controller.Get(userId);

        // Assert
        var okResult = Assert.IsType<ActionResult<UserDto>>(result);
        var okObjectResult = Assert.IsType<OkObjectResult>(okResult.Result);
        Assert.Same(userDto, okObjectResult.Value);
    }

    [Fact]
    public async Task Get_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserService
            .Setup(s => s.GetUserAsync(userId))
            .ReturnsAsync((UserDto?)null);

        // Act
        var result = await _controller.Get(userId);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Get_CallsServiceWithCorrectId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserService
            .Setup(s => s.GetUserAsync(userId))
            .ReturnsAsync((UserDto?)null);

        // Act
        await _controller.Get(userId);

        // Assert
        _mockUserService.Verify(s => s.GetUserAsync(userId), Times.Once);
    }
}
