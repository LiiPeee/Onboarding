using Microsoft.Extensions.Caching.Memory;
using Onboarding.Domain.Entities;
using Onboarding.Domain.Repositories;

namespace Onboarding.Repositories.Cache;

public class CachedAccountRepository(IAccountRepository inner, IMemoryCache cache) : IAccountRepository
{
    private readonly IAccountRepository _inner = inner;
    private readonly IMemoryCache _cache = cache;

    private static string Key(long id) => $"account:{id}";

    private static DateTimeOffset EndOfDay()
    {
        var now = DateTimeOffset.Now;
        return now.Date.AddDays(1);
    }

    public async Task<Account?> GetByIdAsync(long id)
    {
        if (_cache.TryGetValue(Key(id), out Account? cached))
            return cached;

        var account = await _inner.GetByIdAsync(id);
        if (account is not null)
            _cache.Set(Key(id), account, EndOfDay());

        return account;
    }

    public async Task<Account> AddAsync(Account entity) => await _inner.AddAsync(entity);

    public async Task<bool> UpdateAsync(Account entity)
    {
        var result = await _inner.UpdateAsync(entity);
        _cache.Remove(Key(entity.Id));
        return result;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var result = await _inner.DeleteAsync(id);
        _cache.Remove(Key(id));
        return result;
    }

    public Task<Account?> GetByCpfAsync(string cpf) => _inner.GetByCpfAsync(cpf);

    public Task<IReadOnlyList<Account>> GetAllAsync() => _inner.GetAllAsync();
}
