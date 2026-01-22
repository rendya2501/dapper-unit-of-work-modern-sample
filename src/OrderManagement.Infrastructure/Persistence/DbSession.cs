using OrderManagement.Application.Common;
using System.Data;
using System.Data.Common;

namespace OrderManagement.Infrastructure.Persistence;

/// <summary>
/// データベースセッションの実装クラス。
/// </summary>
/// <param name="connection">
/// 使用するデータベース接続。
/// Connection と Transaction のライフタイムを管理します。
/// </param>
public class DbSession(IDbConnection connection) : IDbSessionManager, IDisposable, IAsyncDisposable
{
    private bool _disposed;

    /// <inheritdoc />
    public IDbConnection Connection => connection;

    /// <inheritdoc />
    public IDbTransaction? Transaction { get; set; }


    /// <summary>
    /// 同期的にリソースを解放します。
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// リソースを解放します。
    /// </summary>
    /// <param name="disposing">マネージドリソースを解放する場合は true。</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            // Transaction を先にクリア
            Transaction?.Dispose();
            Transaction = null;

            // Connection を閉じて破棄
            if (connection.State != ConnectionState.Closed)
            {
                connection.Close();
            }
            connection.Dispose();
        }

        _disposed = true;
    }

    /// <summary>
    /// 非同期的にリソースを解放します。
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        // Transaction を先にクリア
        if (Transaction is DbTransaction dbTrans)
            await dbTrans.DisposeAsync();
        else
            Transaction?.Dispose();

        Transaction = null;

        // Connection を非同期で閉じて破棄
        if (connection.State != ConnectionState.Closed)
        {
            if (connection is DbConnection dbConn)
                await dbConn.CloseAsync();
            else
                connection.Close();
        }

        if (connection is DbConnection dbConnDispose)
            await dbConnDispose.DisposeAsync();
        else
            connection.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
