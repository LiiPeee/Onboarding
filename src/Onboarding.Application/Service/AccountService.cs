using Onboarding.Domain.Entities;
using Onboarding.Domain.Enums;
using Onboarding.Domain.Events;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.UnitOfWork;
using Onboarding.Services.Interfaces;
using Onboarding.Services.Models.Request;
using Onboarding.Services.Models.Response;
using Onboarding.Services.Validators;

namespace Onboarding.Services.Service;

public class AccountService(
    IAccountRepository accounts,
    IOutboxRepository outbox,
    IUnitOfWork unitOfWork) : IAccountService
{
    private readonly IAccountRepository _accounts = accounts;
    private readonly IOutboxRepository _outbox = outbox;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<AccountData> CreateAsync(CreateAccountData data)
    {
        if (string.IsNullOrWhiteSpace(data.Name))
            throw new ArgumentException("Account holder name is required.", nameof(data));

        if (!CpfValidator.IsValid(data.Cpf))
            throw new ArgumentException("Invalid CPF.", nameof(data));

        var cpf = new string(data.Cpf.Where(char.IsDigit).ToArray());

        if (await _accounts.GetByCpfAsync(cpf) is not null)
            throw new InvalidOperationException("An account with this CPF already exists.");

        var account = new Account(data.Name.Trim(), cpf);

        await _accounts.AddAsync(account);
        await _outbox.AddAsync(OutboxEvent.Create(AccountEventTypes.AccountCreated,
            new { account.Id, account.Name, account.Cpf, Status = account.Status.ToString() }));
        await _unitOfWork.CommitAsync();

        return AccountData.FromEntity(account);
    }

    public async Task<AccountData?> GetByIdAsync(long id)
    {
        var account = await _accounts.GetByIdAsync(id);
        return account is null ? null : AccountData.FromEntity(account);
    }

    public async Task<IReadOnlyList<AccountData>> GetAllAsync()
    {
        var accounts = await _accounts.GetAllAsync();
        return accounts.Select(AccountData.FromEntity).ToList();
    }

    public async Task<AccountData> UpdateAsync(long id, UpdateAccountData data)
    {
        var account = await _accounts.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Account {id} not found.");

        if (string.IsNullOrWhiteSpace(data.Name))
            throw new ArgumentException("Account holder name is required.", nameof(data));

        if (!Enum.TryParse<AccountStatus>(data.Status, ignoreCase: true, out var status))
            throw new ArgumentException("Status must be 'Ativa' or 'Inativa'.", nameof(data));

        account.UpdateName(data.Name.Trim());
        if (status == AccountStatus.Ativa) account.Activate(); else account.Deactivate();

        await _accounts.UpdateAsync(account);
        await _outbox.AddAsync(OutboxEvent.Create(AccountEventTypes.AccountUpdated,
            new { account.Id, account.Name, account.Cpf, Status = account.Status.ToString() }));
        await _unitOfWork.CommitAsync();

        return AccountData.FromEntity(account);
    }

    public async Task DeleteAsync(long id)
    {
        var account = await _accounts.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Account {id} not found.");

        await _accounts.DeleteAsync(id);
        await _outbox.AddAsync(OutboxEvent.Create(AccountEventTypes.AccountDeleted,
            new { account.Id, account.Cpf }));
        await _unitOfWork.CommitAsync();
    }
}
