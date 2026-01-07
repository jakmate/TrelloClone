using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moq;

using TrelloClone.Server.Application.Interfaces;
using TrelloClone.Server.Controllers;
using TrelloClone.Shared.DTOs.Column;

using Xunit;

namespace TrelloClone.Server.Tests.Controllers;

public class ColumnsControllerTests
{
    private readonly Mock<IColumnService> _mockService;
    private readonly ColumnsController _controller;

    public ColumnsControllerTests()
    {
        _mockService = new Mock<IColumnService>();
        _controller = new ColumnsController(_mockService.Object);
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
    public async Task GetAll_NullList_ReturnsNotFound()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.GetColumnsForBoardAsync(It.IsAny<Guid>())).ReturnsAsync((List<ColumnDto>?)null!);

        // Act
        var result = await _controller.GetAll(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetAll_ValidRequest_ReturnsOk()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        var columns = new List<ColumnDto> { new ColumnDto { Id = Guid.NewGuid() } };
        _mockService.Setup(x => x.GetColumnsForBoardAsync(It.IsAny<Guid>())).ReturnsAsync(columns);

        // Act
        var result = await _controller.GetAll(Guid.NewGuid());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(columns, okResult.Value);
    }

    [Fact]
    public async Task GetAll_ServiceThrows_Returns500()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.GetColumnsForBoardAsync(It.IsAny<Guid>())).ThrowsAsync(new Exception("Test error"));

        // Act
        var result = await _controller.GetAll(Guid.NewGuid());

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidUserId_ReturnsUnauthorized()
    {
        // Arrange
        SetupUser("invalid");

        // Act
        var result = await _controller.Create(Guid.NewGuid(), new CreateColumnRequest());

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task Create_MismatchedBoardId_ReturnsBadRequest()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        var boardId = Guid.NewGuid();
        var req = new CreateColumnRequest { BoardId = Guid.NewGuid() };

        // Act
        var result = await _controller.Create(boardId, req);

        // Assert
        Assert.IsType<BadRequestResult>(result.Result);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        var boardId = Guid.NewGuid();
        var req = new CreateColumnRequest { BoardId = boardId };
        var dto = new ColumnDto { Id = Guid.NewGuid() };
        _mockService.Setup(x => x.CreateColumnAsync(req)).ReturnsAsync(dto);

        // Act
        var result = await _controller.Create(boardId, req);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(ColumnsController.GetAll), createdResult.ActionName);
        Assert.Equal(dto, createdResult.Value);
    }

    [Fact]
    public async Task Update_InvalidUserId_ReturnsUnauthorized()
    {
        // Arrange
        SetupUser("invalid");

        // Act
        var result = await _controller.Update(Guid.NewGuid(), Guid.NewGuid(), new UpdateColumnRequest());

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task Update_ValidRequest_ReturnsOk()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        var dto = new ColumnDto { Id = Guid.NewGuid() };
        _mockService.Setup(x => x.UpdateColumnAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<UpdateColumnRequest>())).ReturnsAsync(dto);

        // Act
        var result = await _controller.Update(Guid.NewGuid(), Guid.NewGuid(), new UpdateColumnRequest());

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
        var result = await _controller.Delete(Guid.NewGuid());

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Delete_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());

        // Act
        var result = await _controller.Delete(Guid.NewGuid());

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockService.Verify(x => x.DeleteColumnAsync(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task ReorderColumns_InvalidUserId_ReturnsUnauthorized()
    {
        // Arrange
        SetupUser("invalid");

        // Act
        var result = await _controller.ReorderColumns(Guid.NewGuid(), new ReorderColumnsRequest());

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task ReorderColumns_ValidRequest_ReturnsOk()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        var request = new ReorderColumnsRequest { Columns = new List<ColumnPositionDto>() };

        // Act
        var result = await _controller.ReorderColumns(Guid.NewGuid(), request);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockService.Verify(x => x.ReorderColumnsAsync(It.IsAny<Guid>(), request.Columns), Times.Once);
    }
}
