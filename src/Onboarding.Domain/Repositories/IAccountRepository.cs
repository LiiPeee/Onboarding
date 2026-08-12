using Onboarding.Domain.Entities;

namespace Onboarding.Domain.Repositories;

public interface IAccountRepository : IRepositoryBase<Account>
{
    Task<Account?> GetByCpfAsync(string cpf);
    Task<IReadOnlyList<Account>> GetAllAsync();
}
