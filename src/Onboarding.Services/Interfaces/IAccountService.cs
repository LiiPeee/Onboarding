using Onboarding.Services.Models.Request;
using Onboarding.Services.Models.Response;

namespace Onboarding.Services.Interfaces;

public interface IAccountService
{
    Task<AccountData> CreateAsync(CreateAccountData data);
    Task<AccountData?> GetByIdAsync(long id);
    Task<AccountData?> GetByCpfAsync(string cpf);
    Task<IReadOnlyList<AccountData>> GetAllAsync();
    Task<AccountData> UpdateAsync(long id, UpdateAccountData data);
    Task DeleteAsync(long id);
}
