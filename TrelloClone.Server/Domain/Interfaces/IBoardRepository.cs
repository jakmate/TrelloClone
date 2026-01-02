using TrelloClone.Server.Domain.Entities;
using TrelloClone.Shared.DTOs.Board;

namespace TrelloClone.Server.Domain.Interfaces;

public interface IBoardRepository
{
    Task<bool> ExistsAsync(Guid boardId);
    Task<bool> NameExistsAsync(string name, Guid userId);
    Task<Board?> GetByIdAsync(Guid boardId);
    Task<List<Board>> GetAllByUserIdAsync(Guid userId);
    Task UpdatePositionsAsync(List<BoardPositionDto> positions);
    Task<Board?> GetByIdWithMembersAsync(Guid boardId);
    void Add(Board board);
    void Update(Board board);
    void Remove(Board board);
}
