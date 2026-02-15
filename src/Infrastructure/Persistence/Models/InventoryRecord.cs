namespace Infrastructure.Persistence.Models;

/// <summary>
/// Inventory テーブルの永続化契約
/// </summary>
public record InventoryRecord
{
    /// <summary>
    /// 商品ID（主キー）
    /// </summary>
    public int ProductId { get; init; }

    /// <summary>
    /// 商品名
    /// </summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>
    /// 在庫数
    /// </summary>
    public int Stock { get; init; }

    /// <summary>
    /// 単価
    /// </summary>
    public decimal UnitPrice { get; init; }
}
