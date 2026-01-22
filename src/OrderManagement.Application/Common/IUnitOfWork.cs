namespace OrderManagement.Application.Common;

/// <summary>
/// Unit of Work パターンの中核インターフェース
/// </summary>
public interface IUnitOfWork : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// 同期的に新しいトランザクションを開始します。
    /// </summary>
    /// <exception cref="InvalidOperationException">既にトランザクションが開始されている場合にスローされます。</exception>
    void BeginTransaction();

    /// <summary>
    /// 現在のトランザクションでの変更を確定します。
    /// </summary>
    /// <exception cref="InvalidOperationException">トランザクションが開始されていない場合にスローされます。</exception>
    void Commit();

    /// <summary>
    /// 現在のトランザクションでの変更を取り消します。
    /// </summary>
    /// <exception cref="InvalidOperationException">トランザクションが開始されていない場合にスローされます。</exception>
    void Rollback();

    /// <summary>
    /// 非同期的に新しいトランザクションを開始します。
    /// </summary>
    /// <param name="ct">キャンセル通知を受け取るためのトークン。</param>
    /// <returns>非同期操作を表すタスク。</returns>
    /// <exception cref="InvalidOperationException">既にトランザクションが開始されている場合にスローされます。</exception>
    Task BeginTransactionAsync(CancellationToken ct = default);

    /// <summary>
    /// 非同期的に現在のトランザクションでの変更を確定します。
    /// </summary>
    /// <param name="ct">キャンセル通知を受け取るためのトークン。</param>
    /// <returns>非同期操作を表すタスク。</returns>
    /// <exception cref="InvalidOperationException">トランザクションが開始されていない場合にスローされます。</exception>
    Task CommitAsync(CancellationToken ct = default);

    /// <summary>
    /// 非同期的に現在のトランザクションでの変更を取り消します。
    /// </summary>
    /// <param name="ct">キャンセル通知を受け取るためのトークン。</param>
    /// <returns>非同期操作を表すタスク。</returns>
    Task RollbackAsync(CancellationToken ct = default);
}
