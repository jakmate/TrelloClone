using TrelloClone.Server.Domain.Interfaces;

namespace TrelloClone.Server.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _ctx;

    public UnitOfWork(AppDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task SaveChangesAsync()
    {
        await _ctx.SaveChangesAsync();
    }
}
