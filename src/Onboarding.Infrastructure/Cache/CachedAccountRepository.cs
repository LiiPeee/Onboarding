using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Onboarding.Domain.Entities;
using Onboarding.Domain.Repositories;

namespace Onboarding.Infrastructure.Cache;

public class CachedAccountRepository(IAccountRepository inner, IDistributedCache cache) : IAccountRepository
{
    private readonly IAccountRepository _inner = inner;
    private readonly IDistributedCache _cache = cache;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static string Key(long id) => $"account:{id}";
    private static string CpfKey(string cpf) => $"account:cpf:{cpf}";
    private static string AllKey() => "accounts:all";

    private static DistributedCacheEntryOptions EndOfDay()
    {
        var now = DateTimeOffset.Now;
        return new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = now.Date.AddDays(1)
        };
    }

    public async Task<Account?> GetByIdAsync(long id)
    {
        var cached = await _cache.GetStringAsync(Key(id));
        if (cached is not null)
            return JsonSerializer.Deserialize<Account>(cached, JsonOptions);

        var account = await _inner.GetByIdAsync(id);
        if (account is not null)
            await _cache.SetStringAsync(Key(id), JsonSerializer.Serialize(account, JsonOptions), EndOfDay());

        return account;
    }

    public async Task<Account?> GetByCpfAsync(string cpf)
    {
        var cached = await _cache.GetStringAsync(CpfKey(cpf));
        if (cached is not null)
            return JsonSerializer.Deserialize<Account>(cached, JsonOptions);

        var account = await _inner.GetByCpfAsync(cpf);
        if (account is not null)
            await _cache.SetStringAsync(CpfKey(cpf), JsonSerializer.Serialize(account, JsonOptions), EndOfDay());

        return account;
    }

    public async Task<IReadOnlyList<Account>> GetAllAsync()
    {
        var cached = await _cache.GetStringAsync(AllKey());
        if (cached is not null)
            return JsonSerializer.Deserialize<List<Account>>(cached, JsonOptions)!;

        var accounts = await _inner.GetAllAsync();
        await _cache.SetStringAsync(AllKey(), JsonSerializer.Serialize(accounts, JsonOptions), EndOfDay());

        return accounts;
    }

    public async Task<Account> AddAsync(Account entity)
    {
        var result = await _inner.AddAsync(entity);
        await _cache.RemoveAsync(AllKey());
        await _cache.RemoveAsync(CpfKey(entity.Cpf));
        return result;
    }

    public async Task<bool> UpdateAsync(Account entity)
    {
        var result = await _inner.UpdateAsync(entity);
        await _cache.RemoveAsync(Key(entity.Id));
        await _cache.RemoveAsync(CpfKey(entity.Cpf));
        await _cache.RemoveAsync(AllKey());
        return result;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var result = await _inner.DeleteAsync(id);
        var cached = await _cache.GetStringAsync(Key(id));
        if (cached is not null)
        {
            var account = JsonSerializer.Deserialize<Account>(cached, JsonOptions);
            if (account is not null)
                await _cache.RemoveAsync(CpfKey(account.Cpf));
        }
        await _cache.RemoveAsync(Key(id));
        await _cache.RemoveAsync(AllKey());
        return result;
    }
}
