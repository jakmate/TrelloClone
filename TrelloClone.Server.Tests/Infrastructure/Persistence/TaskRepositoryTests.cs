using Microsoft.EntityFrameworkCore;

using TrelloClone.Server.Domain.Entities;
using TrelloClone.Server.Infrastructure.Persistence;
using TrelloClone.Shared.DTOs.Task;

using Xunit;

namespace TrelloClone.Server.Tests.Infrastructure.Persistence;

public sealed class TaskRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly TaskRepository _repository;
    private bool _disposed;

    public TaskRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new TaskRepository(_context);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesAssignedUsersAndColumn()
    {
        // Arrange
        var columnId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var user = new User { Id = userId, Email = "test@test.com", UserName = "test", PasswordHash = "testHash" };
        var task = new TaskItem { Id = taskId, ColumnId = columnId, Name = "Task" };
        task.AssignedUsers.Add(user);

        _context.Columns.Add(new Column { Id = columnId, Title = "Test" });
        _context.Users.Add(user);
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(taskId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Column);
        Assert.Single(result.AssignedUsers);
    }

    [Fact]
    public async Task GetByIdAsync_TaskNotFound_ReturnsNull()
    {
        // Act
        var task = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(task);
    }

    [Fact]
    public async Task ListByColumnAsync_ReturnsTasksOrderedByPosition()
    {
        // Arrange
        var columnId = Guid.NewGuid();
        _context.Columns.Add(new Column { Id = columnId, Title = "Test" });
        _context.Tasks.AddRange(
            new TaskItem { ColumnId = columnId, Name = "Task1", Position = 2 },
            new TaskItem { ColumnId = columnId, Name = "Task2", Position = 1 }
        );
        await _context.SaveChangesAsync();

        // Act
        var tasks = await _repository.ListByColumnAsync(columnId);

        // Assert
        Assert.Equal(2, tasks.Count);
        Assert.Equal("Task2", tasks[0].Name);
        Assert.Equal("Task1", tasks[1].Name);
    }

    [Fact]
    public async Task UpdatePositionsAsync_UpdatesTaskPositions()
    {
        // Arrange
        var task1 = new TaskItem { Id = Guid.NewGuid(), ColumnId = Guid.NewGuid(), Name = "Task1", Position = 1 };
        var task2 = new TaskItem { Id = Guid.NewGuid(), ColumnId = Guid.NewGuid(), Name = "Task2", Position = 2 };
        _context.Tasks.AddRange(task1, task2);
        await _context.SaveChangesAsync();

        var positions = new List<TaskPositionDto>
        {
            new TaskPositionDto { Id = task1.Id, Position = 3 },
            new TaskPositionDto { Id = task2.Id, Position = 1 }
        };

        // Act
        await _repository.UpdatePositionsAsync(positions);
        await _context.SaveChangesAsync();

        // Assert
        var updated1 = await _context.Tasks.FindAsync(task1.Id);
        var updated2 = await _context.Tasks.FindAsync(task2.Id);
        Assert.Equal(3, updated1!.Position);
        Assert.Equal(1, updated2!.Position);
    }

    [Fact]
    public async Task UpdatePositionsAsync_MovesTaskToNewColumn()
    {
        // Arrange
        var oldColumnId = Guid.NewGuid();
        var newColumnId = Guid.NewGuid();
        var task = new TaskItem { Id = Guid.NewGuid(), ColumnId = oldColumnId, Name = "Task", Position = 1 };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var positions = new List<TaskPositionDto>
        {
            new TaskPositionDto { Id = task.Id, Position = 2, ColumnId = newColumnId }
        };

        // Act
        await _repository.UpdatePositionsAsync(positions);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Tasks.FindAsync(task.Id);
        Assert.Equal(newColumnId, updated!.ColumnId);
        Assert.Equal(2, updated.Position);
    }

    [Fact]
    public async Task AssignUsersToTaskAsync_AssignsUsers()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        _context.Tasks.Add(new TaskItem { Id = taskId, ColumnId = Guid.NewGuid(), Name = "Task" });
        _context.Users.AddRange(
            new User { Id = userId1, Email = "user1@test.com", UserName = "user1", PasswordHash = "testHash", },
            new User { Id = userId2, Email = "user2@test.com", UserName = "user2", PasswordHash = "testHash", }
        );
        await _context.SaveChangesAsync();

        // Act
        await _repository.AssignUsersToTaskAsync(taskId, new List<Guid> { userId1, userId2 });
        await _context.SaveChangesAsync();

        // Assert
        var task = await _context.Tasks.Include(t => t.AssignedUsers).FirstAsync(t => t.Id == taskId);
        Assert.Equal(2, task.AssignedUsers.Count);
    }

    [Fact]
    public async Task AssignUsersToTaskAsync_ClearsExistingAssignments()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var oldUserId = Guid.NewGuid();
        var newUserId = Guid.NewGuid();

        _context.Users.AddRange(
            new User { Id = oldUserId, Email = "old@test.com", UserName = "old", PasswordHash = "testHash" },
            new User { Id = newUserId, Email = "new@test.com", UserName = "new", PasswordHash = "testHash", }
        );
        _context.Tasks.Add(new TaskItem { Id = taskId, ColumnId = Guid.NewGuid(), Name = "Task" });
        await _context.SaveChangesAsync();

        // Act
        await _repository.AssignUsersToTaskAsync(taskId, new List<Guid> { newUserId });
        await _context.SaveChangesAsync();

        // Assert
        var task = await _context.Tasks.Include(t => t.AssignedUsers).FirstAsync(t => t.Id == taskId);
        Assert.Single(task.AssignedUsers);
        Assert.Equal(newUserId, task.AssignedUsers.First().Id);
    }

    [Fact]
    public async Task AssignUsersToTaskAsync_TaskNotFound_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _repository.AssignUsersToTaskAsync(Guid.NewGuid(), new List<Guid>()));
    }

    [Fact]
    public async Task Add_AddsTaskToDatabase()
    {
        // Arrange
        var task = new TaskItem { Id = Guid.NewGuid(), ColumnId = Guid.NewGuid(), Name = "Task" };

        // Act
        _repository.Add(task);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await _context.Tasks.FindAsync(task.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task Update_UpdatesTaskInDatabase()
    {
        // Arrange
        var task = new TaskItem { Id = Guid.NewGuid(), ColumnId = Guid.NewGuid(), Name = "OldName" };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Act
        task.Name = "UpdatedName";
        _repository.Update(task);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Tasks.FindAsync(task.Id);
        Assert.Equal("UpdatedName", updated!.Name);
    }

    [Fact]
    public async Task Remove_RemovesTaskFromDatabase()
    {
        // Arrange
        var task = new TaskItem { Id = Guid.NewGuid(), ColumnId = Guid.NewGuid(), Name = "Task" };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Act
        _repository.Remove(task);
        await _context.SaveChangesAsync();

        // Assert
        var deleted = await _context.Tasks.FindAsync(task.Id);
        Assert.Null(deleted);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _context?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
