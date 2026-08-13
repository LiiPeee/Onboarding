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
            options.UseNpgsql(GetConnectionString(configuration)));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = GetRedisConfiguration(configuration);
            options.InstanceName = "onboarding:";
        });

        services.AddScoped<AccountRepository>();
        services.AddScoped<IAccountRepository>(sp =>
            new CachedAccountRepository(sp.GetRequiredService<AccountRepository>(), sp.GetRequiredService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>()));

        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddHostedService<OutboxProcessorService>();

        return services;
    }

    private static string GetConnectionString(IConfiguration configuration)
    {
        var fromConfig = configuration.GetConnectionString("Onboarding");
        if (!string.IsNullOrWhiteSpace(fromConfig))
            return fromConfig;

        var host = configuration["DB_HOST"] ?? "localhost";
        var port = configuration["DB_PORT"] ?? "5432";
        var database = configuration["DB_NAME"] ?? "onboarding";
        var username = configuration["DB_USER"] ?? "postgres";
        var password = configuration["DB_PASSWORD"]
            ?? throw new InvalidOperationException(
                "Database password not configured. Set the DB_PASSWORD environment variable (or ConnectionStrings__Onboarding).");

        return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
    }

    private static string GetRedisConfiguration(IConfiguration configuration)
    {
        var fromConfig = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(fromConfig))
            return fromConfig;

        var host = configuration["REDIS_HOST"] ?? "localhost";
        var port = configuration["REDIS_PORT"] ?? "6379";
        var password = configuration["REDIS_PASSWORD"];

        var connection = $"{host}:{port}";
        if (!string.IsNullOrWhiteSpace(password))
            connection += $",password={password}";

        return connection;
    }
}
