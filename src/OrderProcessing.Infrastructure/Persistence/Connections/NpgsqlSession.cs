using Npgsql;

namespace OrderProcessing.Infrastructure.Persistence.Connections;

public sealed class NpgsqlSession : IAsyncDisposable, IDisposable
{
    private readonly NpgsqlDataSource _dataSource;
    private bool _disposed;

    public NpgsqlSession(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public NpgsqlConnection? Connection { get; private set; }

    public NpgsqlTransaction? Transaction { get; private set; }

    public async Task<NpgsqlConnection> GetOpenConnectionAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (Connection is null)
        {
            Connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        }

        return Connection;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (Transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already open.");
        }

        var connection = await GetOpenConnectionAsync(cancellationToken);
        Transaction = await connection.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        if (Transaction is not null)
        {
            await Transaction.CommitAsync(cancellationToken);
        }

        await DisposeTransactionAsync();
    }

    public async Task RollbackAsync()
    {
        if (Transaction is not null)
        {
            await Transaction.RollbackAsync();
        }

        await DisposeTransactionAsync();
    }

    public async Task CloseAsync()
    {
        await DisposeTransactionAsync();

        if (Connection is not null)
        {
            await Connection.DisposeAsync();
            Connection = null;
        }
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await CloseAsync();
        GC.SuppressFinalize(this);
    }

    private async Task DisposeTransactionAsync()
    {
        if (Transaction is not null)
        {
            await Transaction.DisposeAsync();
            Transaction = null;
        }
    }
}
