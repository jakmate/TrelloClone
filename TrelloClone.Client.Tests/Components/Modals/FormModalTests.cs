using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using TrelloClone.Client.Components.Modals;

using Xunit;

namespace TrelloClone.Client.Tests.Components.Modals;

public class FormModalTests : BunitContext
{
    private class TestModel
    {
        public string Name { get; set; } = "";
    }

    [Fact]
    public void FormModal_ShowFalse_DoesNotRender()
    {
        // Act
        var cut = Render<FormModal>(parameters => parameters
            .Add(p => p.Show, false)
            .Add(p => p.Title, "Test Modal")
            .Add(p => p.Model, new TestModel()));

        // Assert
        Assert.Empty(cut.Markup);
    }

    [Fact]
    public void FormModal_ShowTrue_RendersModal()
    {
        // Act
        var cut = Render<FormModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.Title, "Test Modal")
            .Add(p => p.Model, new TestModel()));

        // Assert
        Assert.Contains("Test Modal", cut.Markup);
        Assert.Contains("modal-wrapper", cut.Markup);
        Assert.Contains("modal-backdrop", cut.Markup);
    }

    [Fact]
    public void FormModal_ClickCloseButton_InvokesOnCancel()
    {
        // Arrange
        var cancelCalled = false;
        var cut = Render<FormModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.Model, new TestModel())
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelCalled = true)));

        // Act
        cut.Find(".close-btn").Click();

        // Assert
        Assert.True(cancelCalled);
    }

    [Fact]
    public void FormModal_ClickCancelButton_InvokesOnCancel()
    {
        // Arrange
        var cancelCalled = false;
        var cut = Render<FormModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.Model, new TestModel())
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelCalled = true)));

        // Act
        cut.Find(".btn-secondary").Click();

        // Assert
        Assert.True(cancelCalled);
    }

    [Fact]
    public void FormModal_SubmitForm_InvokesOnSubmit()
    {
        // Arrange
        EditContext? receivedContext = null;
        var cut = Render<FormModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.Model, new TestModel())
            .Add(p => p.OnSubmit, EventCallback.Factory.Create<EditContext>(this, ctx => receivedContext = ctx)));

        // Act
        cut.Find("form").Submit();

        // Assert
        Assert.NotNull(receivedContext);
    }

    [Fact]
    public void FormModal_IsSubmitting_ShowsSpinner()
    {
        // Act
        var cut = Render<FormModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.Model, new TestModel())
            .Add(p => p.IsSubmitting, true));

        // Assert
        Assert.Contains("spinner", cut.Markup);
    }

    [Fact]
    public void FormModal_IsSubmitting_DisablesSubmitButton()
    {
        // Act
        var cut = Render<FormModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.Model, new TestModel())
            .Add(p => p.IsSubmitting, true));

        // Assert
        var submitButton = cut.Find("button[type='submit']");
        Assert.True(submitButton.HasAttribute("disabled"));
    }

    [Fact]
    public void FormModal_CustomButtonText_Renders()
    {
        // Act
        var cut = Render<FormModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.Model, new TestModel())
            .Add(p => p.CancelText, "Close")
            .Add(p => p.SubmitText, "Save"));

        // Assert
        Assert.Contains("Close", cut.Markup);
        Assert.Contains("Save", cut.Markup);
    }

    [Fact]
    public void FormModal_ChildContent_Renders()
    {
        // Act
        var cut = Render<FormModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.Model, new TestModel())
            .Add(p => p.ChildContent, builder => builder.AddMarkupContent(0, "<p>Custom Content</p>")));

        // Assert
        Assert.Contains("Custom Content", cut.Markup);
    }
}
