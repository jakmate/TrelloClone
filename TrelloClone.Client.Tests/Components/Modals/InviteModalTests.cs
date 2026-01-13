using Bunit;
using Bunit.TestDoubles;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using TrelloClone.Client.Components.Modals;
using TrelloClone.Shared.DTOs.Invitation;
using TrelloClone.Shared.Enums;

using Xunit;

namespace TrelloClone.Client.Tests.Components.Modals;

public class InviteModalTests : BunitContext
{
    public InviteModalTests() { }

    [Fact]
    public void InviteModal_RendersWithCorrectTitle()
    {
        // Act
        var cut = Render<InviteModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.InviteModel, new CreateInvitationRequest()));

        // Assert
        Assert.Contains("Invite User to Board", cut.Markup);
    }

    [Fact]
    public void InviteModal_BindsUsername()
    {
        // Arrange
        var model = new CreateInvitationRequest { Username = "testuser" };
        var cut = Render<InviteModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.InviteModel, model));

        // Act
        var input = cut.Find("#username");

        // Assert
        Assert.Equal("testuser", input.GetAttribute("value"));
    }

    [Fact]
    public void InviteModal_RendersPermissionLevelOptions()
    {
        // Act
        var cut = Render<InviteModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.InviteModel, new CreateInvitationRequest()));

        // Assert
        Assert.Contains("Viewer", cut.Markup);
        Assert.Contains("Editor", cut.Markup);
        Assert.Contains("Admin", cut.Markup);
    }

    [Fact]
    public void InviteModal_BindsPermissionLevel()
    {
        // Arrange
        var model = new CreateInvitationRequest { PermissionLevel = PermissionLevel.Editor };
        var cut = Render<InviteModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.InviteModel, model));

        // Act
        var select = cut.Find("#permissionLevel");

        // Assert
        Assert.Equal(PermissionLevel.Editor.ToString(), select.GetAttribute("value"));
    }

    [Fact]
    public void InviteModal_PassesShowParameter()
    {
        // Arrange
        ComponentFactories.AddStub<FormModal>();

        // Act
        var cut = Render<InviteModal>(parameters => parameters
            .Add(p => p.Show, false)
            .Add(p => p.InviteModel, new CreateInvitationRequest()));

        // Assert
        var formModal = cut.FindComponent<Stub<FormModal>>();
        Assert.False(formModal.Instance.Parameters.Get(x => x.Show));
    }

    [Fact]
    public void InviteModal_InvokesOnCancel()
    {
        // Arrange
        ComponentFactories.AddStub<FormModal>();
        var cancelCalled = false;
        var cut = Render<InviteModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.InviteModel, new CreateInvitationRequest())
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelCalled = true)));

        // Act
        var formModal = cut.FindComponent<Stub<FormModal>>();
        formModal.Instance.Parameters.Get(x => x.OnCancel).InvokeAsync();

        // Assert
        Assert.True(cancelCalled);
    }

    [Fact]
    public void InviteModal_InvokesOnSubmit()
    {
        // Arrange
        ComponentFactories.AddStub<FormModal>();
        EditContext? receivedContext = null;
        var cut = Render<InviteModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.InviteModel, new CreateInvitationRequest())
            .Add(p => p.OnSubmit, EventCallback.Factory.Create<EditContext>(this, ctx => receivedContext = ctx)));

        // Act
        var formModal = cut.FindComponent<Stub<FormModal>>();
        var context = new EditContext(new CreateInvitationRequest());
        formModal.Instance.Parameters.Get(x => x.OnSubmit).InvokeAsync(context);

        // Assert
        Assert.NotNull(receivedContext);
    }
}
