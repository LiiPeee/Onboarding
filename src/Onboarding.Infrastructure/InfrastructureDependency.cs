using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Onboarding.Data;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.UnitOfWork;
using Onboarding.Repositories.Cache;
using Onboarding.Repositories.Outbox;
using Onboarding.Repositories.Persistence;

namespace Onboarding.Repositories;

public static class InfrastructureDependency
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Onboarding")));

        services.AddMemoryCache();

        services.AddScoped<AccountRepository>();
        services.AddScoped<IAccountRepository>(sp =>
            new CachedAccountRepository(sp.GetRequiredService<AccountRepository>(), sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>()));

        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddHostedService<OutboxProcessorService>();

        return services;
    }
}
