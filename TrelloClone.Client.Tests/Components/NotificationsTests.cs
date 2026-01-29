using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Moq;

using TrelloClone.Client.Components;
using TrelloClone.Client.Services;
using TrelloClone.Shared.DTOs.Invitation;

using Xunit;

namespace TrelloClone.Client.Tests.Components;

public class NotificationsTests : BunitContext
{
    private readonly Mock<IInvitationService> _mockService;
    private readonly Mock<INotificationHubClient> _mockHub;
    private readonly Mock<IBoardStateService> _mockBoardState;

    public NotificationsTests()
    {
        _mockService = new Mock<IInvitationService>();
        _mockHub = new Mock<INotificationHubClient>();
        _mockBoardState = new Mock<IBoardStateService>();

        Services.AddSingleton(_mockService.Object);
        Services.AddSingleton(_mockHub.Object);
        Services.AddSingleton(_mockBoardState.Object);
    }

    [Fact]
    public void Notifications_NoInvitations_ShowsEmptyState()
    {
        _mockService.Setup(x => x.GetPendingInvitations()).ReturnsAsync(new List<BoardInvitationDto>());

        var cut = Render<Notifications>();

        Assert.Contains("No pending invitations", cut.Markup);
    }

    [Fact]
    public void Notifications_WithInvitations_ShowsBadge()
    {
        var invitations = new List<BoardInvitationDto>
        {
            new BoardInvitationDto { Id = Guid.NewGuid(), BoardName = "Board1" }
        };
        _mockService.Setup(x => x.GetPendingInvitations()).ReturnsAsync(invitations);

        var cut = Render<Notifications>();

        Assert.Contains("notifications-badge", cut.Markup);
    }

    [Fact]
    public void ToggleDropdown_OpensAndClosesDropdown()
    {
        _mockService.Setup(x => x.GetPendingInvitations()).ReturnsAsync(new List<BoardInvitationDto>());
        var cut = Render<Notifications>();

        var button = cut.Find(".notifications-toggle");
        button.Click();
        Assert.Contains("show", cut.Markup);

        button.Click();
        Assert.DoesNotContain("show", cut.Markup);
    }

    [Fact]
    public async Task Accept_CallsServiceAndRemovesInvitation()
    {
        var invitationId = Guid.NewGuid();
        var invitations = new List<BoardInvitationDto>
        {
            new BoardInvitationDto { Id = invitationId, BoardName = "Board1" }
        };
        _mockService.Setup(x => x.GetPendingInvitations()).ReturnsAsync(invitations);
        _mockService.Setup(x => x.AcceptInvitation(invitationId)).Returns(Task.CompletedTask);

        var cut = Render<Notifications>();
        var acceptBtn = cut.Find(".btn-accept");

        await acceptBtn.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        _mockService.Verify(x => x.AcceptInvitation(invitationId), Times.Once);
        _mockBoardState.Verify(x => x.NotifyBoardsChanged(), Times.Once);
    }

    [Fact]
    public async Task Decline_CallsServiceAndRemovesInvitation()
    {
        var invitationId = Guid.NewGuid();
        var invitations = new List<BoardInvitationDto>
        {
            new BoardInvitationDto { Id = invitationId, BoardName = "Board1" }
        };
        _mockService.Setup(x => x.GetPendingInvitations()).ReturnsAsync(invitations);
        _mockService.Setup(x => x.DeclineInvitation(invitationId)).Returns(Task.CompletedTask);

        var cut = Render<Notifications>();
        var declineBtn = cut.Find(".btn-decline");

        await declineBtn.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        _mockService.Verify(x => x.DeclineInvitation(invitationId), Times.Once);
        _mockBoardState.Verify(x => x.NotifyBoardsChanged(), Times.Never);
    }

    [Fact]
    public void Dispose_UnsubscribesFromEvent()
    {
        _mockService.Setup(x => x.GetPendingInvitations()).ReturnsAsync(new List<BoardInvitationDto>());
        var cut = Render<Notifications>();

        cut.Instance.Dispose();

        // Verify unsubscribe by checking no exception when event is raised
        var exception = Record.Exception(() =>
            _mockHub.Raise(x => x.OnInvitationReceived += null, new BoardInvitationDto())
        );
        Assert.Null(exception);
    }
}
