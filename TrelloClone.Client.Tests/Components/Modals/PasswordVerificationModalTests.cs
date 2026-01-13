using Bunit;
using Bunit.TestDoubles;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using TrelloClone.Client.Components.Modals;

using Xunit;

namespace TrelloClone.Client.Tests.Components.Modals;

public class PasswordVerificationModalTests : BunitContext
{
    public PasswordVerificationModalTests() { }

    [Fact]
    public void PasswordVerificationModal_ShowsMessage()
    {
        // Act
        var cut = Render<PasswordVerificationModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.Message, "Test message"));

        // Assert
        Assert.Contains("Test message", cut.Markup);
    }

    [Fact]
    public void PasswordVerificationModal_ShowsErrorMessage()
    {
        // Act
        var cut = Render<PasswordVerificationModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.ErrorMessage, "Error occurred"));

        // Assert
        Assert.Contains("Error occurred", cut.Markup);
    }

    [Fact]
    public void PasswordVerificationModal_UsesCustomTitle()
    {
        // Act
        var cut = Render<PasswordVerificationModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.Title, "Custom Title"));

        // Assert
        Assert.Contains("Custom Title", cut.Markup);
    }

    [Fact]
    public void PasswordVerificationModal_UsesCustomConfirmText()
    {
        // Arrange
        ComponentFactories.AddStub<FormModal>();

        // Act
        var cut = Render<PasswordVerificationModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.ConfirmText, "Delete"));

        // Assert
        var formModal = cut.FindComponent<Stub<FormModal>>();
        Assert.Equal("Delete", formModal.Instance.Parameters.Get(x => x.SubmitText));
    }

    [Fact]
    public void PasswordVerificationModal_ResetsPasswordOnHide()
    {
        // Act
        var cut = Render<PasswordVerificationModal>(parameters => parameters
            .Add(p => p.Show, true));
        cut.Instance.PasswordModel.Password = "test123";
        cut.Render(parameters => parameters.Add(p => p.Show, false));

        // Assert
        Assert.Empty(cut.Instance.PasswordModel.Password);
    }

    [Fact]
    public void PasswordVerificationModal_InvokesOnCancel()
    {
        // Arrange
        ComponentFactories.AddStub<FormModal>();
        var cancelCalled = false;

        // Act
        var cut = Render<PasswordVerificationModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelCalled = true)));
        var formModal = cut.FindComponent<Stub<FormModal>>();
        formModal.Instance.Parameters.Get(x => x.OnCancel).InvokeAsync();

        // Assert
        Assert.True(cancelCalled);
    }

    [Fact]
    public void PasswordVerificationModal_InvokesOnSubmit()
    {
        // Arrange
        ComponentFactories.AddStub<FormModal>();
        EditContext? receivedContext = null;

        // Act
        var cut = Render<PasswordVerificationModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.OnSubmit, EventCallback.Factory.Create<EditContext>(this, ctx => receivedContext = ctx)));
        var formModal = cut.FindComponent<Stub<FormModal>>();
        var context = new EditContext(new PasswordVerificationModal.PasswordVerification());
        formModal.Instance.Parameters.Get(x => x.OnSubmit).InvokeAsync(context);

        // Assert
        Assert.NotNull(receivedContext);
    }

    [Fact]
    public void PasswordVerificationModal_PassesIsSubmitting()
    {
        // Arrange
        ComponentFactories.AddStub<FormModal>();

        // Act
        var cut = Render<PasswordVerificationModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.IsSubmitting, true));
        var formModal = cut.FindComponent<Stub<FormModal>>();

        // Assert
        Assert.True(formModal.Instance.Parameters.Get(x => x.IsSubmitting));
    }
}
