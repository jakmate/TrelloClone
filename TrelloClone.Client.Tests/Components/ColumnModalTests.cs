using Bunit;
using Bunit.TestDoubles;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using TrelloClone.Client.Components;
using TrelloClone.Shared.DTOs.Column;

using Xunit;

namespace TrelloClone.Client.Tests.Components;

public class ColumnModalTests : BunitContext
{
    public ColumnModalTests() { }

    [Fact]
    public void ColumnModal_RendersWithCorrectTitle()
    {
        // Act
        var cut = Render<ColumnModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.ColumnModel, new CreateColumnRequest()));

        // Assert
        Assert.Contains("Create New Column", cut.Markup);
    }

    [Fact]
    public void ColumnModal_BindsColumnTitle()
    {
        // Arrange
        var model = new CreateColumnRequest { Title = "Test Column" };
        var cut = Render<ColumnModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.ColumnModel, model));

        // Act
        var input = cut.Find("#columnTitle");

        // Assert
        Assert.Equal("Test Column", input.GetAttribute("value"));
    }

    [Fact]
    public void ColumnModal_PassesShowParameter()
    {
        // Arrange
        ComponentFactories.AddStub<FormModal>();

        // Act
        var cut = Render<ColumnModal>(parameters => parameters
            .Add(p => p.Show, false)
            .Add(p => p.ColumnModel, new CreateColumnRequest()));

        // Assert
        var formModal = cut.FindComponent<Stub<FormModal>>();
        Assert.False(formModal.Instance.Parameters.Get(x => x.Show));
    }

    [Fact]
    public void ColumnModal_PassesIsSubmitting()
    {
        // Arrange
        ComponentFactories.AddStub<FormModal>();

        // Act
        var cut = Render<ColumnModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.ColumnModel, new CreateColumnRequest())
            .Add(p => p.IsSubmitting, true));

        // Assert
        var formModal = cut.FindComponent<Stub<FormModal>>();
        Assert.True(formModal.Instance.Parameters.Get(x => x.IsSubmitting));
    }

    [Fact]
    public void ColumnModal_InvokesOnCancel()
    {
        // Arrange
        ComponentFactories.AddStub<FormModal>();
        var cancelCalled = false;
        var cut = Render<ColumnModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.ColumnModel, new CreateColumnRequest())
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelCalled = true)));

        // Act
        var formModal = cut.FindComponent<Stub<FormModal>>();
        formModal.Instance.Parameters.Get(x => x.OnCancel).InvokeAsync();

        // Assert
        Assert.True(cancelCalled);
    }

    [Fact]
    public void ColumnModal_InvokesOnSubmit()
    {
        // Arrange
        ComponentFactories.AddStub<FormModal>();
        EditContext? receivedContext = null;
        var cut = Render<ColumnModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.ColumnModel, new CreateColumnRequest())
            .Add(p => p.OnSubmit, EventCallback.Factory.Create<EditContext>(this, ctx => receivedContext = ctx)));

        // Act
        var formModal = cut.FindComponent<Stub<FormModal>>();
        var context = new EditContext(new CreateColumnRequest());
        formModal.Instance.Parameters.Get(x => x.OnSubmit).InvokeAsync(context);

        // Assert
        Assert.NotNull(receivedContext);
    }
}
