using OrderProcessing.Application.Abstractions.Persistence;

namespace OrderProcessing.Infrastructure.Persistence.Connections;

public sealed class NpgsqlUnitOfWork : IUnitOfWork
{
    private readonly NpgsqlSession _session;

    public NpgsqlUnitOfWork(NpgsqlSession session)
    {
        _session = session;
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        await _session.BeginTransactionAsync(cancellationToken);

        try
        {
            await action(cancellationToken);
            await _session.CommitAsync(cancellationToken);
        }
        catch
        {
            await _session.RollbackAsync();
            throw;
        }
        finally
        {
            await _session.CloseAsync();
        }
    }
}
