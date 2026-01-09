using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moq;

using TrelloClone.Server.Application.Interfaces;
using TrelloClone.Server.Controllers;
using TrelloClone.Shared.DTOs.Board;
using TrelloClone.Shared.Enums;

using Xunit;

namespace TrelloClone.Server.Tests.Controllers;

public class BoardsControllerTests
{
    private readonly Mock<IBoardService> _mockService;
    private readonly BoardsController _controller;

    public BoardsControllerTests()
    {
        _mockService = new Mock<IBoardService>();
        _controller = new BoardsController(_mockService.Object);
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
    public async Task Create_ValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        var dto = new BoardDto { Id = Guid.NewGuid(), Name = "Test" };
        _mockService.Setup(x => x.CreateBoardAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(dto);

        // Act
        var result = await _controller.Create(new CreateBoardRequest { Name = "Test" });

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(BoardsController.Get), createdResult.ActionName);
    }

    [Fact]
    public async Task Create_ServiceThrows_ReturnsBadRequest()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.CreateBoardAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("Error"));

        // Act
        var result = await _controller.Create(new CreateBoardRequest { Name = "Test" });

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Error", badRequest.Value);
    }

    [Fact]
    public async Task Update_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());

        // Act
        var result = await _controller.Update(Guid.NewGuid(), new UpdateBoardRequest { Name = "" });

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Board name cannot be empty.", badRequest.Value);
    }

    [Fact]
    public async Task Update_BoardNotFound_ReturnsNotFound()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.UpdateBoardAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>()))
            .ThrowsAsync(new KeyNotFoundException("Not found"));

        // Act
        var result = await _controller.Update(Guid.NewGuid(), new UpdateBoardRequest { Name = "Test" });

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Not found", notFound.Value);
    }

    [Fact]
    public async Task Update_Unauthorized_ReturnsForbid()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.UpdateBoardAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>()))
            .ThrowsAsync(new UnauthorizedAccessException("Unauthorized"));

        // Act
        var result = await _controller.Update(Guid.NewGuid(), new UpdateBoardRequest { Name = "Test" });

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Update_Conflict_ReturnsConflict()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.UpdateBoardAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("Conflict"));

        // Act
        var result = await _controller.Update(Guid.NewGuid(), new UpdateBoardRequest { Name = "Test" });

        // Assert
        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal("Conflict", conflict.Value);
    }

    [Fact]
    public async Task Update_ValidRequest_ReturnsOk()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        var dto = new BoardDto { Id = Guid.NewGuid() };
        _mockService.Setup(x => x.UpdateBoardAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(dto);

        // Act
        var result = await _controller.Update(Guid.NewGuid(), new UpdateBoardRequest { Name = "Test" });

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(dto, okResult.Value);
    }

    [Fact]
    public async Task ReorderBoards_ValidRequest_ReturnsOk()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        var request = new ReorderBoardsRequest { Boards = new List<BoardPositionDto>() };

        // Act
        var result = await _controller.ReorderBoards(request);

        // Assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task ReorderBoards_Unauthorized_ReturnsForbid()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.ReorderBoardsAsync(It.IsAny<List<BoardPositionDto>>(), It.IsAny<Guid>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.ReorderBoards(new ReorderBoardsRequest());

        // Assert
        Assert.IsType<ForbidResult>(result);
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
    }

    [Fact]
    public async Task Delete_NotFound_ReturnsNotFound()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.DeleteBoardAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ThrowsAsync(new KeyNotFoundException());

        // Act
        var result = await _controller.Delete(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task LeaveBoard_ValidRequest_ReturnsOk()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());

        // Act
        var result = await _controller.LeaveBoard(Guid.NewGuid());

        // Assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task LeaveBoard_InvalidOperation_ReturnsBadRequest()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.LeaveBoardAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("Error"));

        // Act
        var result = await _controller.LeaveBoard(Guid.NewGuid());

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Error", badRequest.Value);
    }

    [Fact]
    public async Task Get_BoardExists_ReturnsOk()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        var dto = new BoardDto { Id = Guid.NewGuid() };
        _mockService.Setup(x => x.GetBoardAsync(It.IsAny<Guid>())).ReturnsAsync(dto);

        // Act
        var result = await _controller.Get(Guid.NewGuid());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(dto, okResult.Value);
    }

    [Fact]
    public async Task Get_BoardNotFound_ReturnsNotFound()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.GetBoardAsync(It.IsAny<Guid>())).ReturnsAsync((BoardDto?)null);

        // Act
        var result = await _controller.Get(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetBoards_HasBoards_ReturnsOk()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        var boards = new[] { new BoardDto { Id = Guid.NewGuid() } };
        _mockService.Setup(x => x.GetAllBoardsAsync(It.IsAny<Guid>())).ReturnsAsync(boards);

        // Act
        var result = await _controller.GetBoards();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(boards, okResult.Value);
    }

    [Fact]
    public async Task GetBoards_NoBoards_ReturnsEmptyArray()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.GetAllBoardsAsync(It.IsAny<Guid>())).ReturnsAsync((BoardDto[]?)null);

        // Act
        var result = await _controller.GetBoards();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Empty((BoardDto[])okResult.Value!);
    }

    [Fact]
    public async Task GetUserPermission_ReturnsPermission()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.GetUserPermissionAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(PermissionLevel.Editor);

        // Act
        var result = await _controller.GetUserPermission(Guid.NewGuid());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(PermissionLevel.Editor, okResult.Value);
    }

    [Fact]
    public async Task CreateBoardFromTemplate_ValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        var dto = new BoardDto { Id = Guid.NewGuid() };
        _mockService.Setup(x => x.CreateBoardFromTemplateAsync(It.IsAny<CreateBoardFromTemplateRequest>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.CreateBoardFromTemplate(new CreateBoardFromTemplateRequest());

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task IsOwner_ReturnsOwnerStatus()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.IsOwnerAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(true);

        // Act
        var result = await _controller.IsOwner(Guid.NewGuid());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True((bool)okResult.Value!);
    }

    [Fact]
    public async Task Delete_Unauthorized_ReturnsForbid()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.DeleteBoardAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ThrowsAsync(new UnauthorizedAccessException("Unauthorized"));

        // Act
        var result = await _controller.Delete(Guid.NewGuid());

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public void GetCurrentUserId_InvalidUserId_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        SetupUser("invalid-guid");

        // Act & Assert
        var ex = Assert.Throws<UnauthorizedAccessException>(() =>
            _controller.Create(new CreateBoardRequest()).GetAwaiter().GetResult());
        Assert.Contains("User is not authenticated or invalid ID", ex.Message);
    }

    [Fact]
    public async Task CreateBoardFromTemplate_ServiceThrows_ReturnsBadRequest()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.CreateBoardFromTemplateAsync(It.IsAny<CreateBoardFromTemplateRequest>()))
            .ThrowsAsync(new Exception("Template error"));

        // Act
        var result = await _controller.CreateBoardFromTemplate(new CreateBoardFromTemplateRequest());

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Template error", badRequest.Value);
    }

    [Fact]
    public async Task GetBoards_ServiceThrows_ReturnsBadRequest()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.GetAllBoardsAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("Error"));

        // Act
        var result = await _controller.GetBoards();

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Error", badRequest.Value);
    }

    [Fact]
    public async Task Get_ServiceThrows_ReturnsBadRequest()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        _mockService.Setup(x => x.GetBoardAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("Error"));

        // Act
        var result = await _controller.Get(Guid.NewGuid());

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Error", badRequest.Value);
    }
}
