using Onboarding.Domain.Entities;
using Onboarding.Models.Entities;

namespace Onboarding.Domain.Repositories;

public interface IAccountRepository : IRepositoryBase<Account>
{
    Task<Account?> GetByCpfAsync(string cpf);
    Task<PaginatedResult<Account>> GetAllAsync(int page, int pageSize);
}
