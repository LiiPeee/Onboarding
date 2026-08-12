using System.Text.Json;

namespace Onboarding.Domain.Entities;

public class OutboxEvent : BaseEntity
{
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime? ProcessedAt { get; private set; }
    public void MarkProcessed() => ProcessedAt = DateTime.UtcNow;
}
