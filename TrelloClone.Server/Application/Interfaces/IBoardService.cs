using TrelloClone.Shared.DTOs.Board;
using TrelloClone.Shared.Enums;

namespace TrelloClone.Server.Application.Interfaces;

public interface IBoardService
{
    Task<BoardDto> CreateBoardAsync(string name, Guid ownerId);
    Task<BoardDto> UpdateBoardAsync(Guid boardId, string newName, Guid userId);
    Task DeleteBoardAsync(Guid boardId, Guid userId);
    Task LeaveBoardAsync(Guid boardId, Guid userId);
    Task<bool> IsOwnerAsync(Guid boardId, Guid userId);
    Task<BoardDto?> GetBoardAsync(Guid boardId);
    Task<BoardDto[]?> GetAllBoardsAsync(Guid ownerId);
    Task<PermissionLevel> GetUserPermissionAsync(Guid boardId, Guid userId);
    Task ReorderBoardsAsync(List<BoardPositionDto> positions, Guid userId);
    Task<BoardDto> CreateBoardFromTemplateAsync(CreateBoardFromTemplateRequest request);
}
