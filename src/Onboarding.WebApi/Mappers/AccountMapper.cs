using Onboarding.Services.Models.Request;
using Onboarding.Services.Models.Response;
using Onboarding.WebApi.Models.Request;
using Onboarding.WebApi.Models.Response;

namespace Onboarding.WebApi.Mappers;

public static class AccountMapper
{
    public static CreateAccountData ToData(this CreateAccountRequest request) => new()
    {
        Name = request.Name,
        Cpf = request.Cpf
    };

    public static UpdateAccountData ToData(this UpdateAccountRequest request) => new()
    {
        Name = request.Name,
        Status = request.Status
    };

    public static AccountResponse ToResponse(this AccountData data) => new()
    {
        Id = data.Id,
        Name = data.Name,
        Cpf = data.Cpf,
        Status = data.Status,
        CreatedAt = data.CreatedAt,
        UpdatedAt = data.UpdatedAt
    };
}
