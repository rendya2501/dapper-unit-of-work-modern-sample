using Microsoft.Extensions.Logging;
using OrderManagement.Application.Common;
using OrderManagement.Domain.Common.Results;
using System.Data;
using System.Data.Common;

namespace OrderManagement.Infrastructure.Persistence;

/// <summary>
/// <see cref="IUnitOfWork"/> の具象実装クラス。
/// <see cref="IDbSessionManager"/> を通じてトランザクションのライフサイクルを制御します。
/// </summary> 
public class UnitOfWork(
    IDbSessionManager sessionManager,
    ILogger<UnitOfWork> logger) : IUnitOfWork
{
    private bool _disposed;

    public async Task<OperationResult<T>> ExecuteInTransactionAsync<T>(
        Func<Task<OperationResult<T>>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            await EnsureConnectionOpenAsync(cancellationToken);
            sessionManager.Transaction = await BeginTransactionAsync(cancellationToken);

            logger.LogDebug(
                "Transaction started for operation {OperationType}",
                typeof(T).Name);

            // 操作を実行
            var result = await operation();

            // Resultの状態に応じて自動判定
            if (result.IsSuccess)
            {
                await CommitTransactionAsync(sessionManager.Transaction, cancellationToken);
                logger.LogInformation(
                    "Transaction committed successfully for {OperationType}",
                    typeof(T).Name);
            }
            else
            {
                await RollbackTransactionAsync(sessionManager.Transaction, cancellationToken);
                logger.LogWarning(
                    "Transaction rolled back due to business failure: {ErrorMessage}",
                    result.ErrorMessage);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            if (sessionManager.Transaction != null)
            {
                await RollbackTransactionAsync(sessionManager.Transaction, CancellationToken.None);
                logger.LogWarning("Transaction rolled back due to cancellation");
            }
            throw;
        }
        catch (Exception ex)
        {
            if (sessionManager.Transaction != null)
            {
                await RollbackTransactionAsync(sessionManager.Transaction, CancellationToken.None);
                logger.LogError(
                    ex,
                    "Transaction rolled back due to exception in {OperationType}",
                    typeof(T).Name);
            }
            throw;
        }
        finally
        {
            if (sessionManager.Transaction != null)
            {
                await DisposeTransactionAsync(sessionManager.Transaction);
                sessionManager.Transaction = null;
            }
        }
    }

    public async Task<OperationResult> ExecuteInTransactionAsync(
        Func<Task<OperationResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            await EnsureConnectionOpenAsync(cancellationToken);
            sessionManager.Transaction = await BeginTransactionAsync(cancellationToken);

            logger.LogDebug("Transaction started");

            var result = await operation();

            if (result.IsSuccess)
            {
                await CommitTransactionAsync(sessionManager.Transaction, cancellationToken);
                logger.LogInformation("Transaction committed successfully");
            }
            else
            {
                await RollbackTransactionAsync(sessionManager.Transaction, cancellationToken);
                logger.LogWarning(
                    "Transaction rolled back due to business failure: {ErrorMessage}",
                    result.ErrorMessage);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            if (sessionManager.Transaction != null)
            {
                await RollbackTransactionAsync(sessionManager.Transaction, CancellationToken.None);
                logger.LogWarning("Transaction rolled back due to cancellation");
            }
            throw;
        }
        catch (Exception ex)
        {
            if (sessionManager.Transaction != null)
            {
                await RollbackTransactionAsync(sessionManager.Transaction, CancellationToken.None);
                logger.LogError(ex, "Transaction rolled back due to exception");
            }
            throw;
        }
        finally
        {
            if (sessionManager.Transaction != null)
            {
                await DisposeTransactionAsync(sessionManager.Transaction);
                sessionManager.Transaction = null;
            }
        }
    }

    private async Task EnsureConnectionOpenAsync(CancellationToken cancellationToken)
    {
        if (sessionManager.Connection.State == ConnectionState.Closed)
        {
            await OpenConnection();

            logger.LogDebug("Database connection opened");
        }
        else if (sessionManager.Connection.State == ConnectionState.Broken)
        {
            sessionManager.Connection.Close();

            await OpenConnection();

            logger.LogWarning("Database connection was broken and has been reopened");
        }

        async Task OpenConnection()
        {
            if (sessionManager.Connection is DbConnection dbConnection)
            {
                await dbConnection.OpenAsync(cancellationToken);
            }
            else
            {
                sessionManager.Connection.Open();
            }
        }   
    }

    private async Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        if (sessionManager.Connection is DbConnection dbConnection)
        {
            return await dbConnection.BeginTransactionAsync(cancellationToken);
        }

        return sessionManager.Connection.BeginTransaction();
    }

    private static async Task CommitTransactionAsync(
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (transaction is DbTransaction dbTransaction)
        {
            await dbTransaction.CommitAsync(cancellationToken);
        }
        else
        {
            transaction.Commit();
        }
    }

    private async Task RollbackTransactionAsync(
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        try
        {
            if (transaction is DbTransaction dbTransaction)
            {
                await dbTransaction.RollbackAsync(cancellationToken);
            }
            else
            {
                transaction.Rollback();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during transaction rollback");
        }
    }

    private async Task DisposeTransactionAsync(IDbTransaction transaction)
    {
        try
        {
            if (transaction is DbTransaction dbTransaction)
            {
                await dbTransaction.DisposeAsync();
            }
            else
            {
                transaction.Dispose();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during transaction dispose");
        }
    }

    // ============================================================
    // 標準Dispose Pattern実装
    // ============================================================

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
            // マネージドリソースの解放

            // コミットされずに Dispose された場合はロールバック
            if (sessionManager.Transaction != null)
            {
                logger.LogWarning(
                    "UnitOfWork is being disposed with an active transaction. Rolling back.");

                try
                {
                    sessionManager.Transaction.Rollback();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error rolling back transaction during disposal");
                }
                finally
                {
                    try
                    {
                        sessionManager.Transaction.Dispose();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error disposing transaction");
                    }

                    sessionManager.Transaction = null;
                }
            }

            // 接続を閉じて破棄
            try
            {
                if (sessionManager.Connection.State != ConnectionState.Closed)
                {
                    sessionManager.Connection.Close();
                }

                sessionManager.Connection.Dispose();
                logger.LogDebug("UnitOfWork disposed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error disposing database connection");
            }
        }

        // アンマネージドリソースの解放
        // (この実装では該当なし)

        _disposed = true;
    }

    /// <summary>
    /// 非同期でリソースを解放します。
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 非同期リソース解放の実装
    /// </summary>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_disposed)
            return;

        // コミットされずに Dispose された場合はロールバック
        if (sessionManager.Transaction != null)
        {
            logger.LogWarning(
                "UnitOfWork is being disposed asynchronously with an active transaction. Rolling back.");

            try
            {
                if (sessionManager.Transaction is DbTransaction dbTransaction)
                {
                    await dbTransaction.RollbackAsync();
                }
                else
                {
                    sessionManager.Transaction.Rollback();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error rolling back transaction during async disposal");
            }
            finally
            {
                try
                {
                    if (sessionManager.Transaction is DbTransaction dbTrans)
                    {
                        await dbTrans.DisposeAsync();
                    }
                    else
                    {
                        sessionManager.Transaction.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error disposing transaction asynchronously");
                }

                sessionManager.Transaction = null;
            }
        }

        // 接続を閉じて破棄
        try
        {
            if (sessionManager.Connection.State != ConnectionState.Closed)
            {
                if (sessionManager.Connection is DbConnection dbConnection)
                {
                    await dbConnection.CloseAsync();
                }
                else
                {
                    sessionManager.Connection.Close();
                }
            }

            if (sessionManager.Connection is DbConnection asyncConnection)
            {
                await asyncConnection.DisposeAsync();
            }
            else
            {
                sessionManager.Connection.Dispose();
            }

            logger.LogDebug("UnitOfWork disposed asynchronously");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error disposing database connection asynchronously");
        }
    }

    /// <summary>
    /// ファイナライザー
    /// </summary>
    ~UnitOfWork()
    {
        Dispose(false);
    }
}