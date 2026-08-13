using FluentAssertions;
using Moq;
using Onboarding.Domain.Entities;
using Onboarding.Domain.Events;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.UnitOfWork;
using Onboarding.Services.Models.Request;
using Onboarding.Services.Service;
using Xunit;

namespace Onboarding.Tests.Application;

public class AccountServiceTests
{
    private readonly Mock<IAccountRepository> _accounts = new();
    private readonly Mock<IOutboxRepository> _outbox = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private AccountService CreateSut() => new(_accounts.Object, _outbox.Object, _uow.Object);

    [Fact]
    public async Task CreateAsync_ValidData_PersistsAccountAndOutboxEvent()
    {
        var sut = CreateSut();

        var result = await sut.CreateAsync(new CreateAccountData { Name = "Felipe", Cpf = "529.982.247-25" });

        result.Name.Should().Be("Felipe");
        result.Status.Should().Be("Ativa");
        _accounts.Verify(r => r.AddAsync(It.Is<Account>(a => a.Cpf == "52998224725")), Times.Once);
        _outbox.Verify(o => o.AddAsync(It.Is<OutboxEvent>(e => e.EventType == AccountEventTypes.AccountCreated)), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InvalidCpf_ThrowsArgumentException()
    {
        var sut = CreateSut();

        var act = () => sut.CreateAsync(new CreateAccountData { Name = "Felipe", Cpf = "123" });

        await act.Should().ThrowAsync<ArgumentException>();
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DuplicateCpf_ThrowsInvalidOperationException()
    {
        _accounts.Setup(r => r.GetByCpfAsync("52998224725"))
            .ReturnsAsync(new Account { Name = "Outro", Cpf = "52998224725" });
        var sut = CreateSut();

        var act = () => sut.CreateAsync(new CreateAccountData { Name = "Felipe", Cpf = "52998224725" });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        var sut = CreateSut();

        var result = await sut.GetByIdAsync(99);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ExistingAccount_UpdatesAndPublishesEvent()
    {
        var account = new Account { Name = "Felipe", Cpf = "52998224725" };
        _accounts.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(account);
        _accounts.Setup(r => r.UpdateAsync(account)).ReturnsAsync(true);
        var sut = CreateSut();

        var result = await sut.UpdateAsync(1, new UpdateAccountData { Name = "Felipe N.", Status = "Inativa" });

        result.Name.Should().Be("Felipe N.");
        result.Status.Should().Be("Inativa");
        _outbox.Verify(o => o.AddAsync(It.Is<OutboxEvent>(e => e.EventType == AccountEventTypes.AccountUpdated)), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsKeyNotFoundException()
    {
        var sut = CreateSut();

        var act = () => sut.UpdateAsync(99, new UpdateAccountData { Name = "X", Status = "Ativa" });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_ExistingAccount_DeletesAndPublishesEvent()
    {
        var account = new Account { Name = "Felipe", Cpf = "52998224725" };
        _accounts.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(account);
        _accounts.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);
        var sut = CreateSut();

        await sut.DeleteAsync(1);

        _accounts.Verify(r => r.DeleteAsync(1), Times.Once);
        _outbox.Verify(o => o.AddAsync(It.Is<OutboxEvent>(e => e.EventType == AccountEventTypes.AccountDeleted)), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsKeyNotFoundException()
    {
        var sut = CreateSut();

        var act = () => sut.DeleteAsync(99);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
