using Domain.Inventory;
using Domain.Orders;
using Infrastructure.Persistence.Models;

namespace Infrastructure.Persistence.Mappers;

/// <summary>
/// Order ドメインモデルと OrderRecord 永続化モデルの双方向マッパー
/// </summary>
/// <remarks>
/// <para><strong>責務</strong></para>
/// <list type="bullet">
/// <item>ドメインモデル → 永続化モデル（保存時）</item>
/// <item>永続化モデル → ドメインモデル（取得時）</item>
/// <item>Value Object ↔ プリミティブ型の変換</item>
/// </list>
/// 
/// <para><strong>設計のポイント</strong></para>
/// <list type="bullet">
/// <item>internal クラスなので Infrastructure 層内でのみ使用可能</item>
/// <item>ドメインモデルの internal メソッド（SetId, SetDetails）を呼び出せる</item>
/// <item>変換ロジックを1箇所に集約</item>
/// </list>
/// </remarks>
internal static class OrderMapper
{
    /// <summary>
    /// ドメインモデルを永続化モデルに変換します（保存用）
    /// </summary>
    /// <param name="order">ドメインモデル</param>
    /// <returns>永続化モデル（OrderRecord + OrderDetailRecords）</returns>
    public static (OrderRecord OrderRecord, IEnumerable<OrderDetailRecord> DetailRecords) ToRecords(Order order)
    {
        var orderRecord = new OrderRecord
        {
            Id = (int)order.Id,     // Value Object → int
            CustomerId = order.CustomerId,
            CreatedAt = order.CreatedAt
        };

        var detailRecords = order.Details
            .Select(d => new OrderDetailRecord
            {
                Id = 0,  // AUTO_INCREMENT用
                OrderId = (int)order.Id,
                ProductId = (int)d.ProductId,   // Value Object → int
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice
            });

        return (orderRecord, detailRecords);
    }

    /// <summary>
    /// 永続化モデルをドメインモデルに変換します（取得用）
    /// </summary>
    /// <param name="orderRecord">注文レコード</param>
    /// <param name="detailRecords">注文明細レコードのリスト</param>
    /// <returns>ドメインモデル</returns>
    public static Order ToDomain(OrderRecord orderRecord, IEnumerable<OrderDetailRecord> detailRecords)
    {
        // プライベートコンストラクタで作成（internal メソッド使用）
        var orderResult = Order.Create(orderRecord.CustomerId);
        if (orderResult.IsFailure)
        {
            throw new InvalidOperationException(
                $"Failed to restore Order from database: {orderResult.Error!.Description}");
        }

        var order = orderResult.Value;
        order.SetId(new OrderId(orderRecord.Id));  // int → Value Object
        order.SetCreatedAt(orderRecord.CreatedAt);

        // 注文明細を復元
        var details = detailRecords
            .Select(d => OrderDetail.Restore(
                new ProductId(d.ProductId),  // int → Value Object
                d.Quantity,
                d.UnitPrice));

        order.SetDetails(details);

        return order;
    }
}