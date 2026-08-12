using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.UnitOfWork;

namespace Onboarding.Repositories.Outbox;

public class OutboxProcessorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorService> _logger;
    private readonly TimeSpan _pollInterval;

    public OutboxProcessorService(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessorService> logger)
        : this(scopeFactory, logger, TimeSpan.FromSeconds(5)) { }

    public OutboxProcessorService(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessorService> logger, TimeSpan pollInterval)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _pollInterval = pollInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingAsync(stoppingToken);
            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var pending = await outbox.GetPendingAsync(batchSize: 20);
        foreach (var e in pending)
        {
            _logger.LogInformation("Publishing event {EventType} for consumers (fraud-prevention, cards): {Payload}",
                e.EventType, e.Payload);
        }

        if (pending.Count > 0)
        {
            await outbox.MarkProcessedAsync(pending);
            await unitOfWork.CommitAsync(stoppingToken);
        }
    }
}
