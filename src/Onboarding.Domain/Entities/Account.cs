using Onboarding.Domain.Enums;

namespace Onboarding.Domain.Entities;

public class Account : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Cpf { get; private set; } = string.Empty;
    public AccountStatus Status { get; private set; } = AccountStatus.Ativa;

    private Account() { } // EF Core

    public Account(string name, string cpf)
    {
        Name = name;
        Cpf = cpf;
        Status = AccountStatus.Ativa;
    }

    public void UpdateName(string name) { Name = name; UpdatedAt = DateTime.UtcNow; }
    public void Activate() { Status = AccountStatus.Ativa; UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() { Status = AccountStatus.Inativa; UpdatedAt = DateTime.UtcNow; }
}
