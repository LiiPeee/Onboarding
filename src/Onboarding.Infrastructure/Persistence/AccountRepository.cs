using Microsoft.EntityFrameworkCore;
using Onboarding.Data;
using Onboarding.Domain.Entities;
using Onboarding.Domain.Repositories;

namespace Onboarding.Repositories.Persistence;

public class AccountRepository(AppDbContext context) : IAccountRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Account> AddAsync(Account entity)
    {
        await _context.Accounts.AddAsync(entity);
        return entity;
    }

    public Task<Account?> GetByIdAsync(long id)
        => _context.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);

    public Task<Account?> GetByCpfAsync(string cpf)
        => _context.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Cpf == cpf);

    public async Task<IReadOnlyList<Account>> GetAllAsync()
        => await _context.Accounts.AsNoTracking().OrderBy(a => a.Id).ToListAsync();

    public Task<bool> UpdateAsync(Account entity)
    {
        _context.Accounts.Update(entity);
        return Task.FromResult(true);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var account = await _context.Accounts.FindAsync(id);
        if (account is null) return false;
        _context.Accounts.Remove(account);
        return true;
    }
}
