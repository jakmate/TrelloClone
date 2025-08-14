using Microsoft.AspNetCore.SignalR.Client;
using TrelloClone.Shared.DTOs;
using TrelloClone.Shared.DTOs.SignalR;

public class BoardHubClient
{
    private readonly HubConnection _hubConnection;
    private readonly ILogger<BoardHubClient> _logger;
    private string? _currentBoardId;

    public BoardHubClient(HubConnection hubConnection, ILogger<BoardHubClient> logger)
    {
        _hubConnection = hubConnection;
        _logger = logger;
        SetupEventHandlers();
    }

    // Events for UI to subscribe to
    public event Action<string, string>? UserJoinedBoard;
    public event Action<string, string>? UserLeftBoard;
    public event Action<TaskDragInfo>? TaskDragStarted;
    public event Action<string>? TaskDragEnded;
    public event Action<TaskMoveInfo>? TaskMoved;
    public event Action<ColumnDragInfo>? ColumnDragStarted;
    public event Action<string>? ColumnDragEnded;
    public event Action<ColumnMoveInfo>? ColumnMoved;
    public event Action<TaskDto>? TaskCreated;
    public event Action<TaskDto>? TaskUpdated;
    public event Action<string, string>? TaskDeleted;
    public event Action<ColumnDto>? ColumnCreated;
    public event Action<ColumnDto>? ColumnUpdated;
    public event Action<string>? ColumnDeleted;
    public event Action<string, string, string, string>? UserStartedEditing;
    public event Action<string, string, string>? UserStoppedEditing;

    private void SetupEventHandlers()
    {
        _hubConnection.On<UserBoardEvent>("UserJoinedBoard", user =>
            UserJoinedBoard?.Invoke(user.UserId, user.UserName));

        _hubConnection.On<UserBoardEvent>("UserLeftBoard", user =>
            UserLeftBoard?.Invoke(user.UserId, user.UserName));

        _hubConnection.On<TaskDragInfo>("TaskDragStarted", args =>
            TaskDragStarted?.Invoke(args));

        _hubConnection.On<string>("TaskDragEnded", args =>
            TaskDragEnded?.Invoke(args));

        _hubConnection.On<TaskMoveInfo>("TaskMoved", args =>
            TaskMoved?.Invoke(args));

        _hubConnection.On<ColumnDragInfo>("ColumnDragStarted", args =>
            ColumnDragStarted?.Invoke(args));

        _hubConnection.On<string>("ColumnDragEnded", args =>
            ColumnDragEnded?.Invoke(args));

        _hubConnection.On<ColumnMoveInfo>("ColumnMoved", args =>
            ColumnMoved?.Invoke(args));

        _hubConnection.On<TaskDto>("TaskCreated", args =>
            TaskCreated?.Invoke(args));

        _hubConnection.On<TaskDto>("TaskUpdated", args =>
            TaskUpdated?.Invoke(args));

        _hubConnection.On<TaskDeleteInfo>("TaskDeleted", deleteInfo =>
            TaskDeleted?.Invoke(deleteInfo.TaskId, deleteInfo.ColumnId));

        _hubConnection.On<ColumnDto>("ColumnCreated", args =>
            ColumnCreated?.Invoke(args));

        _hubConnection.On<ColumnDto>("ColumnUpdated", args =>
            ColumnUpdated?.Invoke(args));

        _hubConnection.On<string>("ColumnDeleted", args =>
            ColumnDeleted?.Invoke(args));

        _hubConnection.On<UserEditInfo>("UserStartedEditing", editInfo =>
            UserStartedEditing?.Invoke(editInfo.UserId, editInfo.UserName, editInfo.ItemType, editInfo.ItemId));

        _hubConnection.On<UserStopEditInfo>("UserStoppedEditing", editInfo =>
            UserStoppedEditing?.Invoke(editInfo.UserId, editInfo.ItemType, editInfo.ItemId));
    }

    public async Task JoinBoardAsync(string boardId)
    {
        try
        {
            if (_hubConnection.State != HubConnectionState.Connected)
            {
                await _hubConnection.StartAsync();
            }

            await _hubConnection.InvokeAsync("JoinBoard", boardId);
            _currentBoardId = boardId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining board {BoardId}", boardId);
            throw;
        }
    }

    public async Task LeaveBoardAsync(string boardId)
    {
        try
        {
            await _hubConnection.InvokeAsync("LeaveBoard", boardId);
            _currentBoardId = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving board {BoardId}", boardId);
        }
    }

    // Drag and drop methods
    public async Task NotifyTaskDragStartedAsync(string taskId, string userId, string userName)
    {
        if (_currentBoardId == null) return;

        try
        {
            await _hubConnection.InvokeAsync("TaskDragStarted", _currentBoardId, new TaskDragInfo
            {
                TaskId = taskId,
                UserId = userId,
                UserName = userName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying task drag started");
        }
    }

    public async Task NotifyTaskDragEndedAsync(string taskId)
    {
        if (_currentBoardId == null) return;

        try
        {
            await _hubConnection.InvokeAsync("TaskDragEnded", _currentBoardId, taskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying task drag ended");
        }
    }

    public async Task NotifyTaskMovedAsync(TaskMoveInfo moveInfo)
    {
        if (_currentBoardId == null) return;

        try
        {
            await _hubConnection.InvokeAsync("TaskMoved", _currentBoardId, moveInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying task moved");
        }
    }

    public async Task NotifyColumnDragStartedAsync(string columnId, string userId, string userName)
    {
        if (_currentBoardId == null) return;

        try
        {
            await _hubConnection.InvokeAsync("ColumnDragStarted", _currentBoardId, new ColumnDragInfo
            {
                ColumnId = columnId,
                UserId = userId,
                UserName = userName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying column drag started");
        }
    }

    public async Task NotifyColumnDragEndedAsync(string columnId)
    {
        if (_currentBoardId == null) return;

        try
        {
            await _hubConnection.InvokeAsync("ColumnDragEnded", _currentBoardId, columnId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying column drag ended");
        }
    }

    public async Task NotifyColumnMovedAsync(ColumnMoveInfo moveInfo)
    {
        if (_currentBoardId == null) return;

        try
        {
            await _hubConnection.InvokeAsync("ColumnMoved", _currentBoardId, moveInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying column moved");
        }
    }

    // CRUD notifications
    public async Task NotifyTaskCreatedAsync(TaskDto task)
    {
        if (_currentBoardId == null) return;

        try
        {
            await _hubConnection.InvokeAsync("TaskCreated", _currentBoardId, task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying task created");
        }
    }

    public async Task NotifyTaskUpdatedAsync(TaskDto task)
    {
        if (_currentBoardId == null) return;

        try
        {
            await _hubConnection.InvokeAsync("TaskUpdated", _currentBoardId, task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying task updated");
        }
    }

    public async Task NotifyTaskDeletedAsync(string taskId, string columnId)
    {
        if (_currentBoardId == null) return;

        try
        {
            await _hubConnection.InvokeAsync("TaskDeleted", _currentBoardId, taskId, columnId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying task deleted");
        }
    }

    public async Task NotifyColumnCreatedAsync(ColumnDto column)
    {
        if (_currentBoardId == null) return;

        try
        {
            await _hubConnection.InvokeAsync("ColumnCreated", _currentBoardId, column);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying column created");
        }
    }

    public async Task NotifyColumnUpdatedAsync(ColumnDto column)
    {
        if (_currentBoardId == null) return;

        try
        {
            await _hubConnection.InvokeAsync("ColumnUpdated", _currentBoardId, column);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying column updated");
        }
    }

    public async Task NotifyColumnDeletedAsync(string columnId)
    {
        if (_currentBoardId == null) return;

        try
        {
            await _hubConnection.InvokeAsync("ColumnDeleted", _currentBoardId, columnId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying column deleted");
        }
    }

    // Editing state notifications
    public async Task NotifyUserStartedEditingAsync(string itemType, string itemId)
    {
        if (_currentBoardId == null) return;

        try
        {
            await _hubConnection.InvokeAsync("UserStartedEditing", _currentBoardId, itemType, itemId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying user started editing");
        }
    }

    public async Task NotifyUserStoppedEditingAsync(string itemType, string itemId)
    {
        if (_currentBoardId == null) return;

        try
        {
            await _hubConnection.InvokeAsync("UserStoppedEditing", _currentBoardId, itemType, itemId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying user stopped editing");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_currentBoardId != null)
        {
            await LeaveBoardAsync(_currentBoardId);
        }

        await _hubConnection.DisposeAsync();
    }
}
