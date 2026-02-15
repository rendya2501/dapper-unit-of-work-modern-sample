using Domain.Inventory;
using Infrastructure.Persistence.Models;

namespace Infrastructure.Persistence.Mappers;

/// <summary>
/// Inventory ドメインモデルと InventoryRecord 永続化モデルの双方向マッパー
/// </summary>
internal static class InventoryMapper
{
    /// <summary>
    /// ドメインモデルを永続化モデルに変換します（保存用）
    /// </summary>
    /// <param name="inventory">ドメインモデル</param>
    /// <returns>永続化モデル</returns>
    public static InventoryRecord ToRecord(Inventory inventory)
    {
        return new InventoryRecord
        {
            ProductId = (int)inventory.ProductId,  // Value Object → int
            ProductName = inventory.ProductName,
            Stock = inventory.Stock,
            UnitPrice = inventory.UnitPrice
        };
    }

    /// <summary>
    /// 永続化モデルをドメインモデルに変換します（取得用）
    /// </summary>
    /// <param name="record">永続化モデル</param>
    /// <returns>ドメインモデル</returns>
    public static Inventory ToDomain(InventoryRecord record)
    {
        return Inventory.Restore(
            new ProductId(record.ProductId),  // int → Value Object
            record.ProductName,
            record.Stock,
            record.UnitPrice);
    }
}