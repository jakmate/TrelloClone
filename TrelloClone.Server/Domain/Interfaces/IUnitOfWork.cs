namespace TrelloClone.Server.Domain.Interfaces;

public interface IUnitOfWork
{
    Task SaveChangesAsync();
}
