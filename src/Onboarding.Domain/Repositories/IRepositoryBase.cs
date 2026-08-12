namespace Onboarding.Domain.Repositories;

public interface IRepositoryBase<T> where T : class
{
    Task<T> AddAsync(T entity);
    Task<T?> GetByIdAsync(long id);
    Task<bool> UpdateAsync(T entity);
    Task<bool> DeleteAsync(long id);
}
