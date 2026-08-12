using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Onboarding.Domain.Entities;
using Onboarding.Domain.Repositories;
using Onboarding.Repositories.Cache;
using Xunit;

namespace Onboarding.Tests.Infrastructure;

public class CachedAccountRepositoryTests
{
    private readonly Mock<IAccountRepository> _inner = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    private CachedAccountRepository CreateSut() => new(_inner.Object, _cache);

    [Fact]
    public async Task GetByIdAsync_SecondCall_ReturnsFromCacheWithoutHittingDatabase()
    {
        var account = new Account("Felipe", "52998224725") { Id = 1 };
        _inner.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(account);
        var sut = CreateSut();

        var first = await sut.GetByIdAsync(1);
        var second = await sut.GetByIdAsync(1);

        first.Should().BeSameAs(second);
        _inner.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_InvalidatesCache()
    {
        var account = new Account("Felipe", "52998224725") { Id = 1 };
        _inner.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(account);
        _inner.Setup(r => r.UpdateAsync(account)).ReturnsAsync(true);
        var sut = CreateSut();

        await sut.GetByIdAsync(1);
        await sut.UpdateAsync(account);
        await sut.GetByIdAsync(1);

        _inner.Verify(r => r.GetByIdAsync(1), Times.Exactly(2));
    }

    [Fact]
    public async Task DeleteAsync_InvalidatesCache()
    {
        var account = new Account("Felipe", "52998224725") { Id = 1 };
        _inner.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(account);
        _inner.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);
        var sut = CreateSut();

        await sut.GetByIdAsync(1);
        await sut.DeleteAsync(1);
        await sut.GetByIdAsync(1);

        _inner.Verify(r => r.GetByIdAsync(1), Times.Exactly(2));
    }
}
