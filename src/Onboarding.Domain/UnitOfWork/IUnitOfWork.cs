namespace Onboarding.Domain.UnitOfWork;

public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
