using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Onboarding.Services.Interfaces;
using Onboarding.Services.Service;

namespace Onboarding.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        => services.AddScoped<IAccountService, AccountService>();
}
