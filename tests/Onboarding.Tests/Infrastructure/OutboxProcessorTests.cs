using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Onboarding.Domain.Entities;
using Onboarding.Domain.Events;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.UnitOfWork;
using Onboarding.Repositories.Outbox;
using Xunit;

namespace Onboarding.Tests.Infrastructure;

public class OutboxProcessorTests
{
    [Fact]
    public async Task ProcessPendingEvents_MarksEventsAsProcessed()
    {
        var outbox = new Mock<IOutboxRepository>();
        var uow = new Mock<IUnitOfWork>();
        var pending = new List<OutboxEvent>
        {
            OutboxEvent.Create(AccountEventTypes.AccountCreated, new { Id = 1 })
        };
        outbox.Setup(o => o.GetPendingAsync(It.IsAny<int>())).ReturnsAsync(pending);
        outbox.Setup(o => o.MarkProcessedAsync(It.IsAny<IEnumerable<OutboxEvent>>()))
            .Callback<IEnumerable<OutboxEvent>>(events => { foreach (var e in events) e.MarkProcessed(); });

        var services = new ServiceCollection()
            .AddSingleton(outbox.Object)
            .AddSingleton(uow.Object)
            .BuildServiceProvider();

        var sut = new OutboxProcessorService(
            services.GetRequiredService<IServiceScopeFactory>(),
            Mock.Of<ILogger<OutboxProcessorService>>(),
            TimeSpan.FromMilliseconds(50));

        await sut.StartAsync(CancellationToken.None);
        await Task.Delay(300);
        await sut.StopAsync(CancellationToken.None);

        pending[0].ProcessedAt.Should().NotBeNull();
        uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
