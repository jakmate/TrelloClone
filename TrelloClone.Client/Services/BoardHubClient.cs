using Microsoft.AspNetCore.SignalR.Client;

using TrelloClone.Shared.DTOs;
using TrelloClone.Shared.DTOs.SignalR;

namespace TrelloClone.Client.Services
{
    public partial class BoardHubClient
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
        public event Action<string, string, bool, string, string>? TaskAssignmentUpdating;

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

            _hubConnection.On<dynamic>("TaskAssignmentUpdating", (assignmentInfo) =>
            {
                TaskAssignmentUpdating?.Invoke(
                    assignmentInfo.TaskId.ToString(),
                    assignmentInfo.UserId.ToString(),
                    (bool)assignmentInfo.IsAssigned,
                    assignmentInfo.UpdatedByUserId.ToString(),
                    assignmentInfo.UpdatedByUserName.ToString()
                );
            });
        }

        public async Task JoinBoardAsync(string boardId)
        {
            try
            {
                if (_currentBoardId != null && _currentBoardId != boardId)
                {
                    await LeaveBoardAsync(_currentBoardId);
                }

                if (_hubConnection.State == HubConnectionState.Disconnected)
                {
                    await _hubConnection.StartAsync();
                }

                _currentBoardId = boardId;
                await _hubConnection.InvokeAsync("JoinBoard", boardId);
            }
            catch (Exception ex)
            {
                _currentBoardId = null;
                Log.JoinBoardError(_logger, ex, boardId);
                throw;
            }
        }

        public async Task LeaveBoardAsync(string boardId)
        {
            try
            {
                _currentBoardId = null;
                if (_hubConnection.State == HubConnectionState.Connected)
                {
                    await _hubConnection.InvokeAsync("LeaveBoard", boardId);
                }
            }
            catch (Exception ex)
            {
                _currentBoardId = boardId;
                Log.LeaveBoardError(_logger, ex, boardId);
            }
        }

        // Drag and drop methods
        public async Task NotifyTaskDragStartedAsync(string taskId, string userId, string userName)
        {
            if (_currentBoardId == null || _hubConnection.State != HubConnectionState.Connected)
            {
                return;
            }

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
                Log.TaskDragStartedError(_logger, ex);
            }
        }

        public async Task NotifyTaskDragEndedAsync(string taskId)
        {
            if (_currentBoardId == null || _hubConnection.State != HubConnectionState.Connected)
            {
                return;
            }

            try
            {
                await _hubConnection.InvokeAsync("TaskDragEnded", _currentBoardId, taskId);
            }
            catch (Exception ex)
            {
                Log.TaskDragEndedError(_logger, ex);
            }
        }

        public async Task NotifyTaskMovedAsync(TaskMoveInfo moveInfo)
        {
            if (_currentBoardId == null || _hubConnection.State != HubConnectionState.Connected)
            {
                return;
            }

            try
            {
                await _hubConnection.InvokeAsync("TaskMoved", _currentBoardId, moveInfo);
            }
            catch (Exception ex)
            {
                Log.TaskMovedError(_logger, ex);
            }
        }

        public async Task NotifyColumnDragStartedAsync(string columnId, string userId, string userName)
        {
            if (_currentBoardId == null || _hubConnection.State != HubConnectionState.Connected)
            {
                return;
            }

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
                Log.ColumnDragStartedError(_logger, ex);
            }
        }

        public async Task NotifyColumnDragEndedAsync(string columnId)
        {
            if (_currentBoardId == null || _hubConnection.State != HubConnectionState.Connected)
            {
                return;
            }

            try
            {
                await _hubConnection.InvokeAsync("ColumnDragEnded", _currentBoardId, columnId);
            }
            catch (Exception ex)
            {
                Log.ColumnDragEndedError(_logger, ex);
            }
        }

        public async Task NotifyColumnMovedAsync(ColumnMoveInfo moveInfo)
        {
            if (_currentBoardId == null || _hubConnection.State != HubConnectionState.Connected)
            {
                return;
            }

            try
            {
                await _hubConnection.InvokeAsync("ColumnMoved", _currentBoardId, moveInfo);
            }
            catch (Exception ex)
            {
                Log.ColumnMovedError(_logger, ex);
            }
        }

        // CRUD notifications
        public async Task NotifyTaskCreatedAsync(TaskDto task)
        {
            if (_currentBoardId == null || _hubConnection.State != HubConnectionState.Connected)
            {
                return;
            }

            try
            {
                await _hubConnection.InvokeAsync("TaskCreated", _currentBoardId, task);
            }
            catch (Exception ex)
            {
                Log.TaskCreatedError(_logger, ex);
            }
        }

        public async Task NotifyTaskUpdatedAsync(TaskDto task)
        {
            if (_currentBoardId == null || _hubConnection.State != HubConnectionState.Connected)
            {
                return;
            }

            try
            {
                await _hubConnection.InvokeAsync("TaskUpdated", _currentBoardId, task);
            }
            catch (Exception ex)
            {
                Log.TaskUpdatedError(_logger, ex);
            }
        }

        public async Task NotifyTaskDeletedAsync(string taskId, string columnId)
        {
            if (_currentBoardId == null || _hubConnection.State != HubConnectionState.Connected)
            {
                return;
            }

            try
            {
                await _hubConnection.InvokeAsync("TaskDeleted", _currentBoardId, taskId, columnId);
            }
            catch (Exception ex)
            {
                Log.TaskDeletedError(_logger, ex);
            }
        }

        public async Task NotifyColumnCreatedAsync(ColumnDto column)
        {
            if (_currentBoardId == null || _hubConnection.State != HubConnectionState.Connected)
            {
                return;
            }

            try
            {
                await _hubConnection.InvokeAsync("ColumnCreated", _currentBoardId, column);
            }
            catch (Exception ex)
            {
                Log.ColumnCreatedError(_logger, ex);
            }
        }

        public async Task NotifyColumnUpdatedAsync(ColumnDto column)
        {
            if (_currentBoardId == null || _hubConnection.State != HubConnectionState.Connected)
            {
                return;
            }

            try
            {
                await _hubConnection.InvokeAsync("ColumnUpdated", _currentBoardId, column);
            }
            catch (Exception ex)
            {
                Log.ColumnUpdatedError(_logger, ex);
            }
        }

        public async Task NotifyColumnDeletedAsync(string columnId)
        {
            if (_currentBoardId == null || _hubConnection.State != HubConnectionState.Connected)
            {
                return;
            }

            try
            {
                await _hubConnection.InvokeAsync("ColumnDeleted", _currentBoardId, columnId);
            }
            catch (Exception ex)
            {
                Log.ColumnDeletedError(_logger, ex);
            }
        }

        // Editing state notifications
        public async Task NotifyUserStartedEditingAsync(string itemType, string itemId)
        {
            if (_currentBoardId == null || _hubConnection.State != HubConnectionState.Connected)
            {
                return;
            }

            try
            {
                await _hubConnection.InvokeAsync("UserStartedEditing", _currentBoardId, itemType, itemId);
            }
            catch (Exception ex)
            {
                Log.UserStartedEditingError(_logger, ex);
            }
        }

        public async Task NotifyUserStoppedEditingAsync(string itemType, string itemId)
        {
            if (_currentBoardId == null || _hubConnection.State != HubConnectionState.Connected)
            {
                return;
            }

            try
            {
                await _hubConnection.InvokeAsync("UserStoppedEditing", _currentBoardId, itemType, itemId);
            }
            catch (Exception ex)
            {
                Log.UserStoppedEditingError(_logger, ex);
            }
        }

        public async Task NotifyTaskAssignmentUpdatingAsync(string taskId, Guid userId, bool isAssigned)
        {
            if (_currentBoardId == null || _hubConnection.State != HubConnectionState.Connected)
            {
                return;
            }

            try
            {
                await _hubConnection.InvokeAsync("TaskAssignmentUpdating",
                    _currentBoardId, taskId, userId.ToString(), isAssigned);
            }
            catch (Exception ex)
            {
                Log.TaskAssignmentUpdatingError(_logger, ex);
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

        private static partial class Log
        {
            [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Error joining board {BoardId}")]
            public static partial void JoinBoardError(ILogger logger, Exception exception, string boardId);

            [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Error leaving board {BoardId}")]
            public static partial void LeaveBoardError(ILogger logger, Exception exception, string boardId);

            [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Error notifying task drag started")]
            public static partial void TaskDragStartedError(ILogger logger, Exception exception);

            [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Error notifying task drag ended")]
            public static partial void TaskDragEndedError(ILogger logger, Exception exception);

            [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Error notifying task moved")]
            public static partial void TaskMovedError(ILogger logger, Exception exception);

            [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Error notifying column drag started")]
            public static partial void ColumnDragStartedError(ILogger logger, Exception exception);

            [LoggerMessage(EventId = 7, Level = LogLevel.Error, Message = "Error notifying column drag ended")]
            public static partial void ColumnDragEndedError(ILogger logger, Exception exception);

            [LoggerMessage(EventId = 8, Level = LogLevel.Error, Message = "Error notifying column moved")]
            public static partial void ColumnMovedError(ILogger logger, Exception exception);

            [LoggerMessage(EventId = 9, Level = LogLevel.Error, Message = "Error notifying task created")]
            public static partial void TaskCreatedError(ILogger logger, Exception exception);

            [LoggerMessage(EventId = 10, Level = LogLevel.Error, Message = "Error notifying task updated")]
            public static partial void TaskUpdatedError(ILogger logger, Exception exception);

            [LoggerMessage(EventId = 11, Level = LogLevel.Error, Message = "Error notifying task deleted")]
            public static partial void TaskDeletedError(ILogger logger, Exception exception);

            [LoggerMessage(EventId = 12, Level = LogLevel.Error, Message = "Error notifying column created")]
            public static partial void ColumnCreatedError(ILogger logger, Exception exception);

            [LoggerMessage(EventId = 13, Level = LogLevel.Error, Message = "Error notifying column updated")]
            public static partial void ColumnUpdatedError(ILogger logger, Exception exception);

            [LoggerMessage(EventId = 14, Level = LogLevel.Error, Message = "Error notifying column deleted")]
            public static partial void ColumnDeletedError(ILogger logger, Exception exception);

            [LoggerMessage(EventId = 15, Level = LogLevel.Error, Message = "Error notifying user started editing")]
            public static partial void UserStartedEditingError(ILogger logger, Exception exception);

            [LoggerMessage(EventId = 16, Level = LogLevel.Error, Message = "Error notifying user stopped editing")]
            public static partial void UserStoppedEditingError(ILogger logger, Exception exception);

            [LoggerMessage(EventId = 17, Level = LogLevel.Error, Message = "Error notifying task assignment update")]
            public static partial void TaskAssignmentUpdatingError(ILogger logger, Exception exception);
        }
    }
}
