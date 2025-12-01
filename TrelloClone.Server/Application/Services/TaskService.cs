using TrelloClone.Server.Domain.Entities;
using TrelloClone.Server.Domain.Interfaces;
using TrelloClone.Shared.DTOs;

namespace TrelloClone.Server.Application.Services;

public class TaskService
{
    private readonly ITaskRepository _tasks;
    private readonly IColumnRepository _columns;
    private readonly IUserRepository _users;
    private readonly IBoardRepository _boards;
    private readonly IUnitOfWork _uow;

    public TaskService(
        ITaskRepository tasks,
        IColumnRepository columns,
        IUserRepository users,
        IBoardRepository boards,
        IUnitOfWork uow)
    {
        _tasks = tasks;
        _columns = columns;
        _users = users;
        _boards = boards;
        _uow = uow;
    }

    public async Task<List<TaskDto>> GetTasksForColumnAsync(Guid columnId)
    {
        var list = await _tasks.ListByColumnAsync(columnId);
        return list.Select(t => new TaskDto
        {
            Id = t.Id,
            Name = t.Name,
            Priority = t.Priority,
            AssignedUserIds = t.AssignedUsers.Select(u => u.Id).ToList(),
            ColumnId = t.ColumnId
        }).ToList();
    }

    public async Task<TaskDto> CreateTaskAsync(CreateTaskRequest req)
    {
        var col = await _columns.GetByIdAsync(req.ColumnId)
                  ?? throw new KeyNotFoundException("Column not found.");

        // Validate that assigned users are board members
        if (req.AssignedUserIds != null && req.AssignedUserIds.Count > 0)
        {
            var boardId = col.BoardId;
            await ValidateBoardMembersAsync(boardId, req.AssignedUserIds);
        }

        var task = new TaskItem
        {
            Name = req.Name,
            Priority = req.Priority,
            ColumnId = req.ColumnId
        };

        _tasks.Add(task);
        await _uow.SaveChangesAsync();

        // Assign users if provided
        if (req.AssignedUserIds != null && req.AssignedUserIds.Count > 0)
        {
            await _tasks.AssignUsersToTaskAsync(task.Id, req.AssignedUserIds);
            await _uow.SaveChangesAsync();
        }

        return new TaskDto
        {
            Id = task.Id,
            Name = task.Name,
            Priority = task.Priority,
            AssignedUserIds = req.AssignedUserIds ?? new List<Guid>(),
            ColumnId = task.ColumnId
        };
    }

    public async Task<TaskDto> UpdateTaskAsync(Guid taskId, UpdateTaskRequest req)
    {
        var task = await _tasks.GetByIdAsync(taskId)
                   ?? throw new KeyNotFoundException("Task not found.");

        if (req.Name is { } n)
        {
            task.Name = n;
        }

        if (req.Priority is { } p)
        {
            task.Priority = p;
        }

        // Update user assignments if provided
        if (req.AssignedUserIds is { } userIds)
        {
            // Get board ID through column
            var column = await _columns.GetByIdAsync(task.ColumnId)
                        ?? throw new KeyNotFoundException("Column not found.");

            if (userIds.Count != 0)
            {
                await ValidateBoardMembersAsync(column.BoardId, userIds);
            }

            await _tasks.AssignUsersToTaskAsync(taskId, userIds);
        }

        await _uow.SaveChangesAsync();

        // Reload task to get updated assignments
        task = await _tasks.GetByIdAsync(taskId) ?? throw new KeyNotFoundException("Task not found.");

        return new TaskDto
        {
            Id = task.Id,
            Name = task.Name,
            Priority = task.Priority,
            AssignedUserIds = task.AssignedUsers.Select(u => u.Id).ToList(),
            ColumnId = task.ColumnId
        };
    }

    public async Task DeleteTaskAsync(Guid taskId)
    {
        var task = await _tasks.GetByIdAsync(taskId)
                   ?? throw new KeyNotFoundException("Task not found.");
        _tasks.Remove(task);
        await _uow.SaveChangesAsync();
    }

    public async Task ReorderTasksAsync(List<TaskPositionDto> positions)
    {
        await _tasks.UpdatePositionsAsync(positions);
        await _uow.SaveChangesAsync();
    }

    public async Task<List<UserDto>> GetAvailableUsersForTaskAsync(Guid columnId)
    {
        var column = await _columns.GetByIdAsync(columnId)
                    ?? throw new KeyNotFoundException("Column not found.");
        var board = await _boards.GetByIdWithMembersAsync(column.BoardId)
                   ?? throw new KeyNotFoundException("Board not found.");

        return board.BoardUsers.Select(bu => new UserDto
        {
            Id = bu.User.Id,
            Email = bu.User.Email,
            UserName = bu.User.UserName
        }).ToList();
    }

    private async Task ValidateBoardMembersAsync(Guid boardId, List<Guid> userIds)
    {
        var board = await _boards.GetByIdWithMembersAsync(boardId)
                   ?? throw new KeyNotFoundException("Board not found.");

        var memberIds = board.BoardUsers.Select(bu => bu.User.Id).ToHashSet();
        var invalidUserIds = userIds.Where(id => !memberIds.Contains(id)).ToList();

        if (invalidUserIds.Count != 0)
        {
            throw new InvalidOperationException(
                $"Users {string.Join(", ", invalidUserIds)} are not members of this board.");
        }
    }
}
