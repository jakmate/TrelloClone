using System.Collections.Concurrent;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

using TrelloClone.Server.Application.Services;
using TrelloClone.Shared.DTOs;
using TrelloClone.Shared.DTOs.SignalR;

namespace TrelloClone.Server.Application.Hubs
{
    [Authorize]
    public partial class BoardHub : Hub
    {
        private readonly BoardService _boardService;
        private readonly ILogger<BoardHub> _logger;

        // Track connected users per board
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _boardUsers = new();

        public BoardHub(BoardService boardService, ILogger<BoardHub> logger)
        {
            _boardService = boardService;
            _logger = logger;
        }

        public async Task JoinBoard(string boardId)
        {
            var userId = Context.UserIdentifier;
            var userName = Context.User?.Identity?.Name ?? "Unknown";

            // Verify user has access to this board
            if (!Guid.TryParse(boardId, out var boardGuid) ||
                !Guid.TryParse(userId, out var userGuid))
            {
                await Clients.Caller.SendAsync("Error", "Invalid board or user ID");
                return;
            }

            try
            {
                // Check user permissions
                var permission = await _boardService.GetUserPermissionAsync(boardGuid, userGuid);

                // Allow any valid permission level (Viewer and above)
                if (permission < PermissionLevel.Viewer)
                {
                    await Clients.Caller.SendAsync("Error", "Access denied to board");
                    return;
                }
            }
            catch
            {
                await Clients.Caller.SendAsync("Error", "Error verifying access permissions");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"Board_{boardId}");

            // Get existing users in this board
            var boardKey = $"Board_{boardId}";
            var boardUsers = _boardUsers.GetOrAdd(boardKey, _ => new ConcurrentDictionary<string, string>());

            // Send existing users to the joining user (only send username)
            foreach (var existingUser in boardUsers)
            {
                await Clients.Caller.SendAsync("UserJoinedBoard", new UserBoardEvent
                {
                    UserId = existingUser.Key,
                    UserName = existingUser.Value
                });
            }

            // Add the new user to tracking
            boardUsers.TryAdd(userId ?? "", userName);

            // Notify others that user joined (only send username)
            await Clients.OthersInGroup(boardKey)
                .SendAsync("UserJoinedBoard", new UserBoardEvent
                {
                    UserId = userId ?? "",
                    UserName = userName
                });

            Log.UserJoinedBoard(_logger, userId, boardId);
        }

        public async Task LeaveBoard(string boardId)
        {
            var userId = Context.UserIdentifier;
            var userName = Context.User?.Identity?.Name ?? "Unknown";

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Board_{boardId}");

            // Remove user from tracking
            var boardKey = $"Board_{boardId}";
            if (_boardUsers.TryGetValue(boardKey, out var boardUsers))
            {
                boardUsers.TryRemove(userId ?? "", out _);

                // Clean up empty board tracking
                if (boardUsers.IsEmpty)
                {
                    _boardUsers.TryRemove(boardKey, out _);
                }
            }

            // Notify others that user left
            await Clients.OthersInGroup(boardKey)
                .SendAsync("UserLeftBoard", new UserBoardEvent
                {
                    UserId = userId ?? "",
                    UserName = userName
                });

            Log.UserLeftBoard(_logger, userId, boardId);
        }

        // Task drag and drop events
        public async Task TaskDragStarted(string boardId, TaskDragInfo dragInfo)
        {
            await Clients.OthersInGroup($"Board_{boardId}")
                .SendAsync("TaskDragStarted", dragInfo);
        }

        public async Task TaskDragEnded(string boardId, string taskId)
        {
            await Clients.OthersInGroup($"Board_{boardId}")
                .SendAsync("TaskDragEnded", taskId);
        }

        public async Task TaskMoved(string boardId, TaskMoveInfo moveInfo)
        {
            await Clients.OthersInGroup($"Board_{boardId}")
                .SendAsync("TaskMoved", moveInfo);
        }

        // Column drag and drop events
        public async Task ColumnDragStarted(string boardId, ColumnDragInfo dragInfo)
        {
            await Clients.OthersInGroup($"Board_{boardId}")
                .SendAsync("ColumnDragStarted", dragInfo);
        }

        public async Task ColumnDragEnded(string boardId, string columnId)
        {
            await Clients.OthersInGroup($"Board_{boardId}")
                .SendAsync("ColumnDragEnded", columnId);
        }

        public async Task ColumnMoved(string boardId, ColumnMoveInfo moveInfo)
        {
            await Clients.OthersInGroup($"Board_{boardId}")
                .SendAsync("ColumnMoved", moveInfo);
        }

        // CRUD operations
        public async Task TaskCreated(string boardId, TaskDto task)
        {
            await Clients.OthersInGroup($"Board_{boardId}")
                .SendAsync("TaskCreated", task);
        }

        public async Task TaskUpdated(string boardId, TaskDto task)
        {
            await Clients.OthersInGroup($"Board_{boardId}")
                .SendAsync("TaskUpdated", task);
        }

        public async Task TaskDeleted(string boardId, string taskId, string columnId)
        {
            await Clients.OthersInGroup($"Board_{boardId}")
                .SendAsync("TaskDeleted", new TaskDeleteInfo { TaskId = taskId, ColumnId = columnId });
        }

        public async Task ColumnCreated(string boardId, ColumnDto column)
        {
            await Clients.OthersInGroup($"Board_{boardId}")
                .SendAsync("ColumnCreated", column);
        }

        public async Task ColumnUpdated(string boardId, ColumnDto column)
        {
            await Clients.OthersInGroup($"Board_{boardId}")
                .SendAsync("ColumnUpdated", column);
        }

        public async Task ColumnDeleted(string boardId, string columnId)
        {
            await Clients.OthersInGroup($"Board_{boardId}")
                .SendAsync("ColumnDeleted", columnId);
        }

        // User editing states
        public async Task UserStartedEditing(string boardId, string itemType, string itemId)
        {
            var userId = Context.UserIdentifier;
            var userName = Context.User?.Identity?.Name ?? "Unknown";

            await Clients.OthersInGroup($"Board_{boardId}")
                .SendAsync("UserStartedEditing", new UserEditInfo
                {
                    UserId = userId ?? "",
                    UserName = userName,
                    ItemType = itemType,
                    ItemId = itemId
                });
        }

        public async Task UserStoppedEditing(string boardId, string itemType, string itemId)
        {
            var userId = Context.UserIdentifier;

            await Clients.OthersInGroup($"Board_{boardId}")
                .SendAsync("UserStoppedEditing", new UserStopEditInfo
                {
                    UserId = userId ?? "",
                    ItemType = itemType,
                    ItemId = itemId
                });
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            var userName = Context.User?.Identity?.Name ?? "Unknown";

            // Remove user from all boards they were in
            foreach (var boardEntry in _boardUsers.ToList())
            {
                if (boardEntry.Value.ContainsKey(userId ?? ""))
                {
                    boardEntry.Value.TryRemove(userId ?? "", out _);

                    // Notify others in the board
                    await Clients.OthersInGroup(boardEntry.Key)
                        .SendAsync("UserLeftBoard", new UserBoardEvent
                        {
                            UserId = userId ?? "",
                            UserName = userName
                        });

                    // Clean up empty board tracking
                    if (boardEntry.Value.IsEmpty)
                    {
                        _boardUsers.TryRemove(boardEntry.Key, out _);
                    }
                }
            }

            Log.UserDisconnected(_logger, userId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task TaskAssignmentUpdating(string boardId, string taskId, string userId, bool isAssigned)
        {
            var currentUserId = Context.UserIdentifier;
            var userName = Context.User?.Identity?.Name ?? "Unknown";

            await Clients.OthersInGroup($"Board_{boardId}")
                .SendAsync("TaskAssignmentUpdating", new
                {
                    TaskId = taskId,
                    UserId = userId,
                    IsAssigned = isAssigned,
                    UpdatedByUserId = currentUserId,
                    UpdatedByUserName = userName
                });
        }

        private static partial class Log
        {
            [LoggerMessage(
                EventId = 1,
                Level = LogLevel.Information,
                Message = "User {UserId} joined board {BoardId}")]
            public static partial void UserJoinedBoard(ILogger logger, string? userId, string boardId);

            [LoggerMessage(
                EventId = 2,
                Level = LogLevel.Information,
                Message = "User {UserId} left board {BoardId}")]
            public static partial void UserLeftBoard(ILogger logger, string? userId, string boardId);

            [LoggerMessage(
                EventId = 3,
                Level = LogLevel.Information,
                Message = "User {UserId} disconnected")]
            public static partial void UserDisconnected(ILogger logger, string? userId);
        }
    }
}
