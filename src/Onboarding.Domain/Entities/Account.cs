using Onboarding.Models.Enums;

namespace Onboarding.Domain.Entities
{
    public class Account
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Cpf { get; set; }
        public AccountStatus Status { get; set; } = AccountStatus.Inativa;
    }
}
