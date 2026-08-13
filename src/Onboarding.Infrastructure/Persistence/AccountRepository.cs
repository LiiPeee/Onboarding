using Microsoft.EntityFrameworkCore;
using Onboarding.Data;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.Entities;
using Onboarding.Models.Entities;

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

    public async Task<PaginatedResult<Account>> GetAllAsync(int page,
    int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Accounts
            .AsNoTracking()
            .OrderBy(x => x.Id);

        var totalItems = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(
            totalItems / (double)pageSize);

        return new PaginatedResult<Account>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }

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
