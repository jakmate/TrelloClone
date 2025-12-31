using Microsoft.EntityFrameworkCore;

using TrelloClone.Server.Domain.Entities;
using TrelloClone.Server.Domain.Interfaces;

namespace TrelloClone.Server.Infrastructure.Persistance;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _ctx;

    public UserRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task<bool> ExistsAsync(Guid userId) =>
        await _ctx.Users.AnyAsync(u => u.Id == userId);

    public async Task<User?> GetByIdAsync(Guid userId) =>
        await _ctx.Users.FirstOrDefaultAsync(u => u.Id == userId);

    public async Task<User?> GetByIdWithBoardsAsync(Guid userId) =>
        await _ctx.Users
                  .Include(u => u.BoardUsers)
                  .ThenInclude(bu => bu.Board)
                  .FirstOrDefaultAsync(u => u.Id == userId);

    public async Task<User?> GetByEmailAsync(string email) =>
        await _ctx.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetByUsernameAsync(string userName) =>
       await _ctx.Users.FirstOrDefaultAsync(u => u.UserName == userName);

    public async Task<List<User>> GetAllAsync() =>
        await _ctx.Users.ToListAsync();

    public void Add(User user) => _ctx.Users.Add(user);

    public void Remove(User user) => _ctx.Users.Remove(user);
}
