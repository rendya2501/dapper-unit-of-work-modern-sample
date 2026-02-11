namespace Web.Api.Contracts.Orders.Responses;

/// <summary>
/// 注文のレスポンスDTO
/// </summary>
/// <param name="Id">注文ID</param>
/// <param name="CustomerId">顧客ID</param>
/// <param name="CreatedAt">注文日時（UTC）</param>
/// <param name="TotalAmount">注文合計金額（注文明細から算出）</param>
/// <param name="Details">注文明細のリスト</param>
public record OrderResponse(
    int Id,
    int CustomerId,
    DateTime CreatedAt,
    decimal TotalAmount,
    IEnumerable<OrderDetailResponse> Details);

/// <summary>
/// 注文明細のレスポンスDTO
/// </summary>
/// <param name="ProductId">商品ID</param>
/// <param name="Quantity">数量</param>
/// <param name="UnitPrice">単価（注文時点の価格）</param>
/// <param name="SubTotal">小計（単価 × 数量）</param>
public record OrderDetailResponse(
    int ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal SubTotal);
