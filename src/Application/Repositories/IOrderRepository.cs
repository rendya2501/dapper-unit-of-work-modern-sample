using Domain.Orders;

namespace Application.Repositories;

/// <summary>
/// 注文リポジトリのインターフェース
/// </summary>
/// <remarks>
/// <para><strong>集約ルートに対するリポジトリ</strong></para>
/// <para>
/// Order は集約ルートであり、OrderDetail を含めて永続化される。
/// OrderDetail 単独のリポジトリは存在しない。
/// </para>
/// 
/// <para><strong>トランザクション管理</strong></para>
/// <para>
/// Repository 自身はトランザクションを開始・終了しない。
/// UnitOfWork が管理する Transaction を利用する。
/// </para>
/// </remarks>
public interface IOrderRepository
{
    /// <summary>
    /// 注文を作成します
    /// </summary>
    /// <param name="orderRecord">注文レコード</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>作成された注文ID</returns>
    Task<int> CreateAsync(
        Order order,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// IDを指定して注文を取得します
    /// </summary>
    Task<Order?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// すべての注文を取得します
    /// </summary>
    Task<IEnumerable<Order>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
