namespace Web.Api.Contracts.Inventory.Responses;

/// <summary>
/// <see cref="Inventory"/> ドメインモデルを <see cref="InventoryResponse"/> に変換する拡張メソッド
/// </summary>
public static class InventoryMappingExtensions
{
    /// <summary>
    /// <see cref="Inventory"/> を <see cref="InventoryResponse"/> に変換します
    /// </summary>
    /// <param name="inventory">変換対象の在庫エンティティ</param>
    /// <returns>在庫情報のレスポンスDTO</returns>
    public static InventoryResponse ToResponse(this Domain.Inventory.Inventory inventory) =>
        new(
            inventory.ProductId,
            inventory.ProductName,
            inventory.Stock,
            inventory.UnitPrice);
}
