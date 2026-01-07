using TrelloClone.Shared.DTOs.Column;
using TrelloClone.Shared.Enums;

namespace TrelloClone.Server.Application.Interfaces;

public interface IColumnService
{
    Task<List<ColumnDto>> GetColumnsForBoardAsync(Guid boardId);
    Task<ColumnDto> CreateColumnAsync(CreateColumnRequest req);
    Task<ColumnDto> UpdateColumnAsync(Guid boardId, Guid columnId, UpdateColumnRequest req);
    Task DeleteColumnAsync(Guid columnId);
    Task ReorderColumnsAsync(Guid boardId, List<ColumnPositionDto> positions);
}
