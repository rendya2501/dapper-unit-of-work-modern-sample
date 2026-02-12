namespace Application.DTOs;

/// <summary>
/// 注文アイテムのDTO
/// </summary>
/// <remarks>
/// Controller層とService層の間でやり取りされるデータ転送オブジェクト。
/// ドメインエンティティではないため、ビジネスロジックは持ちません。
/// </remarks>
/// <param name="ProductId">商品ID</param>
/// <param name="Quantity">数量</param>
public record OrderItem(int ProductId, int Quantity);
