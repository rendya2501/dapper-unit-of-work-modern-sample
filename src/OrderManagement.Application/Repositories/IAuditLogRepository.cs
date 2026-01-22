using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Repositories;

/// <summary>
/// 監査ログリポジトリのインターフェース
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>
    /// 監査ログを作成します
    /// </summary>
    /// <param name="log">作成する監査ログ</param>
    Task CreateAsync(AuditLog log);

    /// <summary>
    /// すべての監査ログを取得します
    /// </summary>
    /// <param name="limit">取得件数の上限（デフォルト: 100）</param>
    /// <returns>監査ログのリスト（新しい順）</returns>
    Task<IEnumerable<AuditLog>> GetAllAsync(int limit = 100);
}
