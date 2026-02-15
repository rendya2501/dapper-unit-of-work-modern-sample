using Domain.Inventory;

namespace Domain.Orders;

/// <summary>
/// 注文明細エンティティ（純粋ドメインモデル）
/// </summary>
/// <remarks>
/// <para><strong>値オブジェクトに近い性質</strong></para>
/// <para>
/// OrderDetail は Order の一部であり、Order なしでは意味を持ちません。
/// Orderを通じてのみ作成・管理されます。
/// </para>
/// </remarks>
public class OrderDetail
{
    public OrderId Id { get; set; }

    /// <summary>
    /// 商品ID（Value Object）
    /// </summary>
    public ProductId ProductId { get; private set; }

    /// <summary>
    /// 数量
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// 単価（注文時点の価格）
    /// </summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>
    /// 小計（計算プロパティ）
    /// </summary>
    public decimal SubTotal => UnitPrice * Quantity;


    /// <summary>
    /// プライベートコンストラクタ
    /// </summary>
    /// <remarks>
    /// ファクトリメソッド Create または Mapper からのみ生成可能。
    /// </remarks>
    private OrderDetail() { }


    /// <summary>
    /// 注文明細を作成します（ファクトリメソッド）
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="quantity">数量</param>
    /// <param name="unitPrice">単価</param>
    /// <returns>新規作成された注文明細</returns>
    public static OrderDetail Create(ProductId productId, int quantity, decimal unitPrice)
    {
        return new OrderDetail
        {
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }

    /// <summary>
    /// 永続化層からの復元用ファクトリメソッド
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="quantity">数量</param>
    /// <param name="unitPrice">単価</param>
    /// <returns>復元された注文明細</returns>
    /// <remarks>
    /// Infrastructure層のMapperから呼び出されます。
    /// </remarks>
    public static OrderDetail Restore(ProductId productId, int quantity, decimal unitPrice)
    {
        return new OrderDetail
        {
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }
}
