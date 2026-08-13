using System.Text.Json;
using Onboarding.Domain.Entities;
using Onboarding.Domain.Enums;
using Onboarding.Domain.Events;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.UnitOfWork;
using Onboarding.Models.Entities;
using Onboarding.Services.Interfaces;
using Onboarding.Services.Models.Request;
using Onboarding.Services.Models.Response;
using Onboarding.Services.Validators;

namespace Onboarding.Services.Service;

public class AccountService(
    IAccountRepository accountRepository,
    IOutboxRepository outboxRepository,
    IUnitOfWork unitOfWork) : IAccountService
{
    private readonly IAccountRepository _accountRepository = accountRepository;
    private readonly IOutboxRepository _outboxRepository = outboxRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<AccountData> CreateAsync(CreateAccountData data)
    {
        if (string.IsNullOrWhiteSpace(data.Name))
            throw new ArgumentException("Account holder name is required.", nameof(data));

        if (!CpfValidator.IsValid(data.Cpf))
            throw new ArgumentException("Invalid CPF.", nameof(data));

        var cpf = CpfValidator.NormalizeCpf(data.Cpf);

        if (await _accountRepository.GetByCpfAsync(cpf) is not null)
            throw new InvalidOperationException("An account with this CPF already exists.");

        var account = new Account() { Name = data.Name.Trim(),Cpf = cpf };

        await _accountRepository.AddAsync(account);
        await _outboxRepository.AddAsync(new OutboxEvent
        {
            EventType = AccountEventTypes.AccountCreated,
            Payload = JsonSerializer.Serialize(new { account.Id, account.Name, account.Cpf, Status = account.Status.ToString() })
        });
        await _unitOfWork.CommitAsync();

        return AccountData.FromEntity(account);
    }

    public async Task<AccountData?> GetByIdAsync(long id)
    {
        var account = await _accountRepository.GetByIdAsync(id);
        return account is null ? null : AccountData.FromEntity(account);
    }

    public async Task<PaginatedResult<AccountData>> GetAllAsync(int page, int pageSize)
    {
        var accounts = await _accountRepository.GetAllAsync(page,pageSize);
        return new PaginatedResult<AccountData> {
            Items = accounts.Items.Select(ac => AccountData.FromEntity(ac)).ToList(),
            Page = accounts.Page,
            PageSize = accounts.PageSize,
            TotalItems = accounts.TotalItems,
            TotalPages = accounts.TotalPages };
    }

    public async Task<AccountData> UpdateAsync(long id, UpdateAccountData data)
    {
        var account = await _accountRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Account {id} not found.");

        if (string.IsNullOrWhiteSpace(data.Name))
            throw new ArgumentException("Account holder name is required.", nameof(data));

        if (!Enum.TryParse<AccountStatus>(data.Status, ignoreCase: true, out var status))
            throw new ArgumentException("Status must be 'Ativa' or 'Inativa'.", nameof(data));

        account.UpdateName(data.Name.Trim());
        if (status == AccountStatus.Ativa) account.Activate(); else account.Deactivate();

        await _accountRepository.UpdateAsync(account);
        await _outboxRepository.AddAsync(new OutboxEvent
        {
            EventType = AccountEventTypes.AccountUpdated,
            Payload = JsonSerializer.Serialize(new { account.Id, account.Name, account.Cpf, Status = account.Status.ToString() })
        });
        await _unitOfWork.CommitAsync();

        return AccountData.FromEntity(account);
    }

    public async Task DeleteAsync(long id)
    {
        var account = await _accountRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Account {id} not found.");

        await _accountRepository.DeleteAsync(id);
        await _outboxRepository.AddAsync(new OutboxEvent
        {
            EventType = AccountEventTypes.AccountDeleted,
            Payload = JsonSerializer.Serialize(new { account.Id, account.Cpf })
        });
        await _unitOfWork.CommitAsync();
    }

    public async Task<AccountData?> GetByCpfAsync(string cpf)
    {
        var account = await _accountRepository.GetByCpfAsync($"{cpf}");
        return account is null ? null : AccountData.FromEntity(account);
    }
}
