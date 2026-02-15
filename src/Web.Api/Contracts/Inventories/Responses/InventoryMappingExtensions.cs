using Domain.Inventory;

namespace Web.Api.Contracts.Inventories.Responses;

/// <summary>
/// <see cref="InventoryEntity"/> ドメインモデルを <see cref="InventoryResponse"/> に変換する拡張メソッド
/// </summary>
public static class InventoryMappingExtensions
{
    /// <summary>
    /// <see cref="Inventory"/> を <see cref="InventoryResponse"/> に変換します
    /// </summary>
    /// <param name="inventory">変換対象の在庫エンティティ</param>
    /// <returns>在庫情報のレスポンスDTO</returns>
    public static InventoryResponse ToResponse(this Inventory inventory) =>
        new(
            (int)inventory.ProductId,
            inventory.ProductName,
            inventory.Stock,
            inventory.UnitPrice);
}
