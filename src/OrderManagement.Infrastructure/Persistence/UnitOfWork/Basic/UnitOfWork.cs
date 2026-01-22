using Microsoft.Extensions.Logging;
using OrderManagement.Application.Common;
using System.Data;
using System.Data.Common;

namespace OrderManagement.Infrastructure.Persistence.UnitOfWork.Basic;

/// <summary>
/// <see cref="IUnitOfWork"/> の具象実装クラス。
/// <see cref="IDbSessionManager"/> を通じてトランザクションのライフサイクルを制御します。
/// </summary> 
public class UnitOfWork(
    IDbSessionManager sessionManager,
    ILogger<UnitOfWork> logger) : IUnitOfWork
{
    private bool _disposed;

    /// <summary>
    /// オブジェクトが既に破棄されているか確認します。
    /// </summary>
    /// <exception cref="ObjectDisposedException">オブジェクトが破棄済みの場合にスローされます。</exception>
    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    /// <inheritdoc />
    public void BeginTransaction()
    {
        ThrowIfDisposed();

        if (sessionManager.Connection.State != ConnectionState.Open)
        {
            sessionManager.Connection.Open();
            logger.LogDebug("Database connection opened.");
        }

        if (sessionManager.Transaction != null)
        {
            var ex = new InvalidOperationException("Transaction is already started.");
            logger.LogError(ex, "Failed to begin transaction: A transaction is already active.");
            throw ex;
        }

        sessionManager.Transaction = sessionManager.Connection.BeginTransaction();
        logger.LogInformation("Transaction started (Hash: {Hash}).", sessionManager.Transaction.GetHashCode());
    }

    /// <inheritdoc />
    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (sessionManager.Connection.State != ConnectionState.Open)
        {
            if (sessionManager.Connection is DbConnection dbConn)
                await dbConn.OpenAsync(ct);
            else
                sessionManager.Connection.Open();

            logger.LogDebug("Database connection opened asynchronously.");
        }

        if (sessionManager.Transaction != null)
        {
            var ex = new InvalidOperationException("Transaction has already been started.");
            logger.LogError(ex, "Failed to begin async transaction: A transaction is already active.");
            throw ex;
        }

        sessionManager.Transaction = sessionManager.Connection is DbConnection dbConnForTrans
            ? await dbConnForTrans.BeginTransactionAsync(ct)
            : sessionManager.Connection.BeginTransaction();

        logger.LogInformation("Async transaction started (Hash: {Hash}).", sessionManager.Transaction.GetHashCode());
    }

    /// <inheritdoc />
    public void Commit()
    {
        ThrowIfDisposed();
        if (sessionManager.Transaction == null) 
            throw new InvalidOperationException("No active transaction to commit.");

        try
        {
            sessionManager.Transaction.Commit();
            logger.LogInformation("Transaction committed (Hash: {Hash}).", sessionManager.Transaction.GetHashCode());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during transaction commit.");
            throw;
        }
        finally
        {
            ClearTransaction();
        }
    }

    /// <inheritdoc />
    public async Task CommitAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (sessionManager.Transaction == null) throw new InvalidOperationException("No active transaction to commit.");

        try
        {
            if (sessionManager.Transaction is DbTransaction dbTrans)
                await dbTrans.CommitAsync(ct);
            else
                sessionManager.Transaction.Commit();

            logger.LogInformation("Async transaction committed (Hash: {Hash}).", sessionManager.Transaction.GetHashCode());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during async transaction commit.");
            throw;
        }
        finally
        {
            await ClearTransactionAsync();
        }
    }

    /// <inheritdoc />
    public void Rollback()
    {
        // 破棄済み、またはトランザクションがない場合は何もしない
        if (_disposed || sessionManager.Transaction == null) return;

        try
        {
            sessionManager.Transaction.Rollback();
            logger.LogWarning("Transaction rolled back.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during transaction rollback.");
        }
        finally
        {
            ClearTransaction();
        }
    }

    /// <inheritdoc />
    public async Task RollbackAsync(CancellationToken ct = default)
    {
        // 破棄済み、またはトランザクションがない場合は何もしない
        if (_disposed || sessionManager.Transaction == null) return;

        try
        {
            if (sessionManager.Transaction is DbTransaction dbTrans)
                await dbTrans.RollbackAsync(ct);
            else
                sessionManager.Transaction.Rollback();

            logger.LogWarning("Async transaction rolled back.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during async transaction rollback.");
        }
        finally
        {
            await ClearTransactionAsync();
        }
    }


    private void ClearTransaction()
    {
        if (sessionManager.Transaction == null) return;

        sessionManager.Transaction.Dispose();
        sessionManager.Transaction = null;
    }

    private async Task ClearTransactionAsync()
    {
        if (sessionManager.Transaction == null) return;

        // Note: DbTransaction.DisposeAsync() does not accept CancellationToken
        if (sessionManager.Transaction is DbTransaction dbTrans)
            await dbTrans.DisposeAsync();
        else
            sessionManager.Transaction.Dispose();

        sessionManager.Transaction = null;
    }


    /// <summary>
    /// トランザクションを破棄し、リソースを解放します。
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// マネージドおよびアンマネージドリソースを解放します。
    /// </summary>
    /// <param name="disposing">マネージドリソースを解放する場合は true。</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            // コミットされずに Dispose された場合はロールバック
            if (sessionManager.Transaction != null)
            {
                logger.LogDebug("UnitOfWork is being disposed with an active transaction. Triggering rollback.");
                Rollback();
            }
        }

        _disposed = true;
    }

    /// <summary>
    /// トランザクションを破棄し、リソースを非同期的に解放します。
    /// コミットされていないトランザクションがある場合はロールバックされます。
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        
        if (sessionManager.Transaction != null)
        {
            logger.LogDebug("UnitOfWork is being disposed asynchronously with an active transaction.");
            await RollbackAsync();
        }
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
