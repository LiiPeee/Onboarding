using Onboarding.Domain.Entities;
using Onboarding.Services.Validators;

namespace Onboarding.Services.Models.Response;

public class AccountData
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public static AccountData FromEntity(Account account) => new()
    {
        Id = account.Id,
        Name = account.Name,
        Cpf = CpfValidator.Mask(account.Cpf),
        Status = account.Status.ToString(),
        CreatedAt = account.CreatedAt,
        UpdatedAt = account.UpdatedAt
    };
}
