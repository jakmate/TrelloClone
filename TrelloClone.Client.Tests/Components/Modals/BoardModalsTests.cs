using Bunit;
using Bunit.TestDoubles;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using TrelloClone.Client.Components.Modals;
using TrelloClone.Shared.DTOs.Board;
using TrelloClone.Shared.Enums;

using Xunit;

namespace TrelloClone.Client.Tests.Components.Modals;

public class BoardModalsTests : BunitContext
{
    public BoardModalsTests() { }

    [Fact]
    public void BoardModals_RendersWithCorrectTitle()
    {
        // Act
        var cut = Render<BoardModals>(parameters => parameters
            .Add(p => p.ShowCreateModal, true)
            .Add(p => p.NewBoard, new CreateBoardRequest()));

        // Assert
        Assert.Contains("Create New Board", cut.Markup);
    }

    [Fact]
    public void BoardModals_BindsBoardName()
    {
        // Arrange
        var model = new CreateBoardRequest { Name = "testboard" };
        var cut = Render<BoardModals>(parameters => parameters
            .Add(p => p.ShowCreateModal, true)
            .Add(p => p.NewBoard, model));

        // Act
        var input = cut.Find("#boardName");

        // Assert
        Assert.Equal("testboard", input.GetAttribute("value"));
    }

    [Fact]
    public void BoardModals_PassesShowCreateModalParameter()
    {
        // Arrange
        ComponentFactories.AddStub<FormModal>();

        // Act
        var cut = Render<BoardModals>(parameters => parameters
            .Add(p => p.ShowCreateModal, false)
            .Add(p => p.NewBoard, new CreateBoardRequest()));

        // Assert
        var formModal = cut.FindComponent<Stub<FormModal>>();
        Assert.False(formModal.Instance.Parameters.Get(x => x.Show));
    }

    [Fact]
    public void BoardModals_InvokesOnHideCreate()
    {
        // Arrange
        ComponentFactories.AddStub<FormModal>();
        var hideCreateCalled = false;
        var cut = Render<BoardModals>(parameters => parameters
            .Add(p => p.ShowCreateModal, true)
            .Add(p => p.NewBoard, new CreateBoardRequest())
            .Add(p => p.OnHideCreate, EventCallback.Factory.Create(this, () => hideCreateCalled = true)));

        // Act
        var formModal = cut.FindComponent<Stub<FormModal>>();
        formModal.Instance.Parameters.Get(x => x.OnCancel).InvokeAsync();

        // Assert
        Assert.True(hideCreateCalled);
    }

    [Fact]
    public void BoardModals_InvokesOnCreateBoard()
    {
        // Arrange
        ComponentFactories.AddStub<FormModal>();
        EditContext? receivedContext = null;
        var cut = Render<BoardModals>(parameters => parameters
            .Add(p => p.ShowCreateModal, true)
            .Add(p => p.NewBoard, new CreateBoardRequest())
            .Add(p => p.OnCreateBoard, EventCallback.Factory.Create<EditContext>(this, ctx => receivedContext = ctx)));

        // Act
        var formModal = cut.FindComponent<Stub<FormModal>>();
        var context = new EditContext(new CreateBoardRequest());
        formModal.Instance.Parameters.Get(x => x.OnSubmit).InvokeAsync(context);

        // Assert
        Assert.NotNull(receivedContext);
    }
}
