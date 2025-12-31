using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moq;

using TrelloClone.Server.Application.Interfaces;
using TrelloClone.Server.Application.Services;
using TrelloClone.Server.Controllers;
using TrelloClone.Shared.DTOs;

using Xunit;

namespace TrelloClone.Server.Tests.Controllers;

public class InvitationsControllerTests
{
    private readonly Mock<IInvitationService> _mockService;
    private readonly InvitationsController _controller;

    public InvitationsControllerTests()
    {
        _mockService = new Mock<IInvitationService>();
        _controller = new InvitationsController(_mockService.Object);
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
    public async Task SendInvitation_InvalidUserId_ReturnsUnauthorized()
    {
        // Arrange
        SetupUser("invalid-guid");
        var dto = new SendInvitationDto { BoardId = Guid.NewGuid(), Username = "test", Permission = PermissionLevel.Viewer };

        // Act
        var result = await _controller.SendInvitation(dto);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task SendInvitation_EmptyUsername_ReturnsBadRequest()
    {
        // Arrange
        SetupUser(Guid.NewGuid().ToString());
        var dto = new SendInvitationDto { BoardId = Guid.NewGuid(), Username = "", Permission = PermissionLevel.Viewer };

        // Act
        var result = await _controller.SendInvitation(dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Username cannot be empty.", badRequest.Value);
    }

    [Fact]
    public async Task SendInvitation_ValidRequest_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUser(userId.ToString());
        var dto = new SendInvitationDto { BoardId = Guid.NewGuid(), Username = "test", Permission = PermissionLevel.Editor };
        _mockService.Setup(x => x.SendInvitation(dto.BoardId, userId, dto.Username, dto.Permission)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SendInvitation(dto);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockService.Verify(x => x.SendInvitation(dto.BoardId, userId, dto.Username, dto.Permission), Times.Once);
    }

    [Fact]
    public async Task SendInvitation_ServiceThrows_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUser(userId.ToString());
        var dto = new SendInvitationDto { BoardId = Guid.NewGuid(), Username = "test", Permission = PermissionLevel.Viewer };
        _mockService.Setup(x => x.SendInvitation(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<PermissionLevel>()))
            .ThrowsAsync(new Exception("Test error"));

        // Act
        var result = await _controller.SendInvitation(dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Error: Test error", badRequest.Value);
    }

    [Fact]
    public async Task AcceptInvitation_InvalidUserId_ReturnsUnauthorized()
    {
        // Arrange
        SetupUser("invalid-guid");

        // Act
        var result = await _controller.AcceptInvitation(Guid.NewGuid());

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task AcceptInvitation_ValidRequest_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        SetupUser(userId.ToString());
        _mockService.Setup(x => x.AcceptInvitation(invitationId, userId)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.AcceptInvitation(invitationId);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockService.Verify(x => x.AcceptInvitation(invitationId, userId), Times.Once);
    }

    [Fact]
    public async Task AcceptInvitation_ServiceThrows_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUser(userId.ToString());
        _mockService.Setup(x => x.AcceptInvitation(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("Test error"));

        // Act
        var result = await _controller.AcceptInvitation(Guid.NewGuid());

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Error: Test error", badRequest.Value);
    }

    [Fact]
    public async Task DeclineInvitation_InvalidUserId_ReturnsUnauthorized()
    {
        // Arrange
        SetupUser("invalid-guid");

        // Act
        var result = await _controller.DeclineInvitation(Guid.NewGuid());

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task DeclineInvitation_ValidRequest_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        SetupUser(userId.ToString());
        _mockService.Setup(x => x.DeclineInvitation(invitationId, userId)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeclineInvitation(invitationId);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockService.Verify(x => x.DeclineInvitation(invitationId, userId), Times.Once);
    }

    [Fact]
    public async Task DeclineInvitation_ServiceThrows_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUser(userId.ToString());
        _mockService.Setup(x => x.DeclineInvitation(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("Test error"));

        // Act
        var result = await _controller.DeclineInvitation(Guid.NewGuid());

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Error: Test error", badRequest.Value);
    }

    [Fact]
    public async Task GetPendingInvitations_ReturnsInvitations()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invitations = new List<BoardInvitationDto>
        {
            new BoardInvitationDto { Id = Guid.NewGuid(), BoardName = "Board 1" }
        };
        _mockService.Setup(x => x.GetPendingInvitations(userId)).ReturnsAsync(invitations);

        // Act
        var result = await _controller.GetPendingInvitations(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInvitations = Assert.IsType<List<BoardInvitationDto>>(okResult.Value);
        Assert.Single(returnedInvitations);
    }
}
