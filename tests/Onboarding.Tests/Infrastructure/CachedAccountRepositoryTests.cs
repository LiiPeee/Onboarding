using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Onboarding.Domain.Entities;
using Onboarding.Domain.Repositories;
using Onboarding.Repositories.Cache;
using Xunit;

namespace Onboarding.Tests.Infrastructure;

public class CachedAccountRepositoryTests
{
    private readonly Mock<IAccountRepository> _inner = new();
    private readonly IDistributedCache _cache = new MemoryDistributedCache(
        Options.Create(new MemoryDistributedCacheOptions()));

    private CachedAccountRepository CreateSut() => new(_inner.Object, _cache);

    [Fact]
    public async Task GetByIdAsync_SecondCall_ReturnsFromCacheWithoutHittingDatabase()
    {
        var account = new Account { Name = "Felipe", Cpf = "52998224725", Id = 1 };
        _inner.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(account);
        var sut = CreateSut();

        var first = await sut.GetByIdAsync(1);
        var second = await sut.GetByIdAsync(1);

        second.Should().BeEquivalentTo(first);
        _inner.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_InvalidatesCache()
    {
        var account = new Account { Name = "Felipe", Cpf = "52998224725", Id = 1 };
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
        var account = new Account { Name = "Felipe", Cpf = "52998224725", Id = 1 };
        _inner.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(account);
        _inner.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);
        var sut = CreateSut();

        await sut.GetByIdAsync(1);
        await sut.DeleteAsync(1);
        await sut.GetByIdAsync(1);

        _inner.Verify(r => r.GetByIdAsync(1), Times.Exactly(2));
    }

    [Fact]
    public async Task GetByCpfAsync_SecondCall_ReturnsFromCacheWithoutHittingDatabase()
    {
        var account = new Account { Name = "Felipe", Cpf = "52998224725", Id = 1 };
        _inner.Setup(r => r.GetByCpfAsync("52998224725")).ReturnsAsync(account);
        var sut = CreateSut();

        var first = await sut.GetByCpfAsync("52998224725");
        var second = await sut.GetByCpfAsync("52998224725");

        second.Should().BeEquivalentTo(first);
        _inner.Verify(r => r.GetByCpfAsync("52998224725"), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_SecondCall_ReturnsFromCacheWithoutHittingDatabase()
    {
        var accounts = new List<Account> { new() { Name = "Felipe", Cpf = "52998224725", Id = 1 } };
        _inner.Setup(r => r.GetAllAsync()).ReturnsAsync(accounts);
        var sut = CreateSut();

        var first = await sut.GetAllAsync();
        var second = await sut.GetAllAsync();

        second.Should().BeEquivalentTo(first);
        _inner.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task AddAsync_InvalidatesAllCache()
    {
        var account = new Account { Name = "Felipe", Cpf = "52998224725", Id = 1 };
        _inner.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Account> { account });
        _inner.Setup(r => r.AddAsync(It.IsAny<Account>())).ReturnsAsync(account);
        var sut = CreateSut();

        await sut.GetAllAsync();
        await sut.AddAsync(new Account { Name = "Novo", Cpf = "11144477735" });
        await sut.GetAllAsync();

        _inner.Verify(r => r.GetAllAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task AddAsync_InvalidatesCpfCacheForAddedAccount()
    {
        var account = new Account { Name = "Felipe", Cpf = "52998224725", Id = 1 };
        _inner.Setup(r => r.GetByCpfAsync("52998224725")).ReturnsAsync(account);
        _inner.Setup(r => r.AddAsync(It.IsAny<Account>())).ReturnsAsync(account);
        var sut = CreateSut();

        await sut.GetByCpfAsync("52998224725");
        await sut.AddAsync(account);
        await sut.GetByCpfAsync("52998224725");

        _inner.Verify(r => r.GetByCpfAsync("52998224725"), Times.Exactly(2));
    }

    [Fact]
    public async Task UpdateAsync_InvalidatesAllAndCpfCache()
    {
        var account = new Account { Name = "Felipe", Cpf = "52998224725", Id = 1 };
        _inner.Setup(r => r.GetByCpfAsync("52998224725")).ReturnsAsync(account);
        _inner.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Account> { account });
        _inner.Setup(r => r.UpdateAsync(account)).ReturnsAsync(true);
        var sut = CreateSut();

        await sut.GetByCpfAsync("52998224725");
        await sut.GetAllAsync();
        await sut.UpdateAsync(account);
        await sut.GetByCpfAsync("52998224725");
        await sut.GetAllAsync();

        _inner.Verify(r => r.GetByCpfAsync("52998224725"), Times.Exactly(2));
        _inner.Verify(r => r.GetAllAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task DeleteAsync_InvalidatesAllAndCpfCache()
    {
        var account = new Account { Name = "Felipe", Cpf = "52998224725", Id = 1 };
        _inner.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(account);
        _inner.Setup(r => r.GetByCpfAsync("52998224725")).ReturnsAsync(account);
        _inner.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Account> { account });
        _inner.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);
        var sut = CreateSut();

        await sut.GetByIdAsync(1);
        await sut.GetByCpfAsync("52998224725");
        await sut.GetAllAsync();
        await sut.DeleteAsync(1);
        await sut.GetByCpfAsync("52998224725");
        await sut.GetAllAsync();

        _inner.Verify(r => r.GetByCpfAsync("52998224725"), Times.Exactly(2));
        _inner.Verify(r => r.GetAllAsync(), Times.Exactly(2));
    }
}
