using Onboarding.Domain.Entities;

namespace Onboarding.Domain.Repositories;

public interface IOutboxRepository
{
    Task AddAsync(OutboxEvent outboxEvent);
    Task<IReadOnlyList<OutboxEvent>> GetPendingAsync(int batchSize);
    Task MarkProcessedAsync(IEnumerable<OutboxEvent> events);
}
