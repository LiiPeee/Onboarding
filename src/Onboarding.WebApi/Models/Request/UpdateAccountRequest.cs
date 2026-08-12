namespace Onboarding.WebApi.Models.Request;

public class UpdateAccountRequest
{
    public required string Name { get; set; }
    public required string Status { get; set; }
}
