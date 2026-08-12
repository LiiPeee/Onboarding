namespace Onboarding.WebApi.Models.Request;

public class CreateAccountRequest
{
    public required string Name { get; set; }
    public required string Cpf { get; set; }
}
