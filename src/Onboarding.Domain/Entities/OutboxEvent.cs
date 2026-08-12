using System.Text.Json;

namespace Onboarding.Domain.Entities;

public class OutboxEvent : BaseEntity
{
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime? ProcessedAt { get; private set; }

    private OutboxEvent() { } // EF Core

    public static OutboxEvent Create(string eventType, object payload) => new()
    {
        EventType = eventType,
        Payload = JsonSerializer.Serialize(payload)
    };

    public void MarkProcessed() => ProcessedAt = DateTime.UtcNow;
}
