using Microsoft.EntityFrameworkCore;
using Onboarding.Data;
using Onboarding.Domain.Entities;
using Onboarding.Domain.Repositories;

namespace Onboarding.Repositories.Persistence;

public class OutboxRepository(AppDbContext context) : IOutboxRepository
{
    private readonly AppDbContext _context = context;

    public async Task AddAsync(OutboxEvent outboxEvent)
        => await _context.OutboxEvents.AddAsync(outboxEvent);

    public async Task<IReadOnlyList<OutboxEvent>> GetPendingAsync(int batchSize)
        => await _context.OutboxEvents
            .Where(e => e.ProcessedAt == null)
            .OrderBy(e => e.Id)
            .Take(batchSize)
            .ToListAsync();

    public Task MarkProcessedAsync(IEnumerable<OutboxEvent> events)
    {
        foreach (var e in events) e.MarkProcessed();
        return Task.CompletedTask;
    }
}
