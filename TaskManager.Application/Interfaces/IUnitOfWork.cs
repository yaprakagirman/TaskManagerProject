namespace TaskManager.Application.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync();
}
