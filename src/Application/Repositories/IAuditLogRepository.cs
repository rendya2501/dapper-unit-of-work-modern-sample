using SharedKernel.Models;

namespace Application.Repositories;

/// <summary>
/// 監査ログリポジトリのインターフェース
/// </summary>
/// <remarks>
/// <para><strong>Shared Kernelパターン</strong></para>
/// <para>
/// AuditLogRecordはShared Kernelとして全層から参照可能です。<br/>
/// これによりDIP（依存関係逆転の原則）を維持しつつ、<br/>
/// 共通のデータ構造を使用できます。
/// </para>
/// </remarks>
public interface IAuditLogRepository
{
    /// <summary>
    /// 監査ログを作成します
    /// </summary>
    /// <param name="log">作成する監査ログ</param>  
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task CreateAsync(
        AuditLogRecord log,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// すべての監査ログを取得します
    /// </summary>
    /// <param name="limit">取得件数の上限（デフォルト: 100）</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>監査ログのリスト（新しい順）</returns>
    Task<IEnumerable<AuditLogRecord>> GetAllAsync(
        int limit = 100,
        CancellationToken cancellationToken = default);
}
