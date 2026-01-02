using Bunit;
using Bunit.TestDoubles;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using TrelloClone.Client.Components;
using TrelloClone.Shared.DTOs.Task;
using TrelloClone.Shared.DTOs.User;
using TrelloClone.Shared.Enums;

using Xunit;

namespace TrelloClone.Client.Tests.Components;

public class TaskModalTests : BunitContext
{
    public TaskModalTests() { }

    [Fact]
    public void TaskModal_RendersWithCorrectTitle()
    {
        // Act
        var cut = Render<TaskModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.TaskModel, new CreateTaskRequest()));

        // Assert
        Assert.Contains("Create New Task", cut.Markup);
    }

    [Fact]
    public void TaskModal_BindsUsername()
    {
        // Arrange
        var model = new CreateTaskRequest { Name = "testtask" };
        var cut = Render<TaskModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.TaskModel, model));

        // Act
        var input = cut.Find("#taskName");

        // Assert
        Assert.Equal("testtask", input.GetAttribute("value"));
    }

    [Fact]
    public void TaskModal_RendersPriorityOptions()
    {
        // Act
        var cut = Render<TaskModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.TaskModel, new CreateTaskRequest()));

        // Assert
        Assert.Contains("Low", cut.Markup);
        Assert.Contains("Medium", cut.Markup);
        Assert.Contains("High", cut.Markup);
    }

    [Fact]
    public void TaskModal_BindsPriority()
    {
        // Arrange
        var model = new CreateTaskRequest { Priority = PriorityLevel.Medium };
        var cut = Render<TaskModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.TaskModel, model));

        // Act
        var select = cut.Find("#taskPriority");

        // Assert
        Assert.Equal(PriorityLevel.Medium.ToString(), select.GetAttribute("value"));
    }

    [Fact]
    public void TaskModal_PassesShowParameter()
    {
        // Arrange
        ComponentFactories.AddStub<FormModal>();

        // Act
        var cut = Render<TaskModal>(parameters => parameters
            .Add(p => p.Show, false)
            .Add(p => p.TaskModel, new CreateTaskRequest()));

        // Assert
        var formModal = cut.FindComponent<Stub<FormModal>>();
        Assert.False(formModal.Instance.Parameters.Get(x => x.Show));
    }

    [Fact]
    public void TaskModal_InvokesOnCancel()
    {
        // Arrange
        ComponentFactories.AddStub<FormModal>();
        var cancelCalled = false;
        var cut = Render<TaskModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.TaskModel, new CreateTaskRequest())
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelCalled = true)));

        // Act
        var formModal = cut.FindComponent<Stub<FormModal>>();
        formModal.Instance.Parameters.Get(x => x.OnCancel).InvokeAsync();

        // Assert
        Assert.True(cancelCalled);
    }

    [Fact]
    public void TaskModal_InvokesOnSubmit()
    {
        // Arrange
        ComponentFactories.AddStub<FormModal>();
        EditContext? receivedContext = null;
        var cut = Render<TaskModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.TaskModel, new CreateTaskRequest())
            .Add(p => p.OnSubmit, EventCallback.Factory.Create<EditContext>(this, ctx => receivedContext = ctx)));

        // Act
        var formModal = cut.FindComponent<Stub<FormModal>>();
        var context = new EditContext(new CreateTaskRequest());
        formModal.Instance.Parameters.Get(x => x.OnSubmit).InvokeAsync(context);

        // Assert
        Assert.NotNull(receivedContext);
    }

    [Fact]
    public void TaskModal_NoAvailableUsers_ShowsLoadingMessage()
    {
        // Act
        var cut = Render<TaskModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.TaskModel, new CreateTaskRequest())
            .Add(p => p.AvailableUsers, null));

        // Assert
        Assert.Contains("Loading available users...", cut.Markup);
    }

    [Fact]
    public void TaskModal_WithAvailableUsers_RendersUserCheckboxes()
    {
        // Arrange
        var users = new List<UserDto>
        {
            new UserDto { Id = Guid.NewGuid(), UserName = "user1", Email = "user1@test.com" },
            new UserDto { Id = Guid.NewGuid(), UserName = "user2", Email = "user2@test.com" }
        };

        // Act
        var cut = Render<TaskModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.TaskModel, new CreateTaskRequest())
            .Add(p => p.AvailableUsers, users));

        // Assert
        Assert.Contains("user1", cut.Markup);
        Assert.Contains("user1@test.com", cut.Markup);
        Assert.Contains("user2", cut.Markup);
        Assert.Contains("user2@test.com", cut.Markup);
    }

    [Fact]
    public void TaskModal_ToggleUserAssignment_AddsUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var users = new List<UserDto>
        {
            new UserDto { Id = userId, UserName = "user1", Email = "user1@test.com" }
        };
        var model = new CreateTaskRequest
        {
            AssignedUserIds = new List<Guid>()
        };

        var cut = Render<TaskModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.TaskModel, model)
            .Add(p => p.AvailableUsers, users));

        // Act
        var checkbox = cut.Find($"#user_{userId}");
        checkbox.Change(true);

        // Assert
        Assert.Contains(userId, model.AssignedUserIds);
    }

    [Fact]
    public void TaskModal_ToggleUserAssignment_RemovesUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var users = new List<UserDto>
        {
            new UserDto { Id = userId, UserName = "user1", Email = "user1@test.com" }
        };
        var model = new CreateTaskRequest { AssignedUserIds = new List<Guid> { userId } };

        var cut = Render<TaskModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.TaskModel, model)
            .Add(p => p.AvailableUsers, users));

        // Act
        var checkbox = cut.Find($"#user_{userId}");
        checkbox.Change(false);

        // Assert
        Assert.DoesNotContain(userId, model.AssignedUserIds);
    }

    [Fact]
    public void TaskModal_PreAssignedUser_CheckboxIsChecked()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var users = new List<UserDto>
        {
            new UserDto { Id = userId, UserName = "user1", Email = "user1@test.com" }
        };
        var model = new CreateTaskRequest { AssignedUserIds = new List<Guid> { userId } };

        // Act
        var cut = Render<TaskModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.TaskModel, model)
            .Add(p => p.AvailableUsers, users));

        // Assert
        var checkbox = cut.Find($"#user_{userId}");
        Assert.True(checkbox.HasAttribute("checked"));
    }

    [Fact]
    public void TaskModal_ToggleUserAssignment_DoesNotAddDuplicate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var users = new List<UserDto>
        {
            new UserDto { Id = userId, UserName = "user1", Email = "user1@test.com" }
        };
        var model = new CreateTaskRequest { AssignedUserIds = new List<Guid> { userId } };

        var cut = Render<TaskModal>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.TaskModel, model)
            .Add(p => p.AvailableUsers, users));

        // Act
        var checkbox = cut.Find($"#user_{userId}");
        checkbox.Change(true);

        // Assert
        Assert.Single(model.AssignedUserIds, id => id == userId);
    }
}
