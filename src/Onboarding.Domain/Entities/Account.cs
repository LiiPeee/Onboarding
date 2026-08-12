using Onboarding.Domain.Enums;

namespace Onboarding.Domain.Entities;

public class Account : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public AccountStatus Status { get; set; } = AccountStatus.Ativa;

    public void UpdateName(string name) { Name = name; UpdatedAt = DateTime.UtcNow; }
    public void Activate() { Status = AccountStatus.Ativa; UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() { Status = AccountStatus.Inativa; UpdatedAt = DateTime.UtcNow; }
}
