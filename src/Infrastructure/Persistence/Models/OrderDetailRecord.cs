namespace Infrastructure.Persistence.Models;

/// <summary>
/// OrderDetails テーブルの永続化契約
/// </summary>
public record OrderDetailRecord
{
    /// <summary>
    /// 明細ID（主キー、AUTO_INCREMENT）
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// 注文ID（外部キー）
    /// </summary>
    /// <remarks>
    /// 例外的に set 可能。親レコード作成後に設定する必要があるため。
    /// </remarks>
    public int OrderId { get; set; }

    /// <summary>
    /// 商品ID
    /// </summary>
    public int ProductId { get; init; }

    /// <summary>
    /// 数量
    /// </summary>
    public int Quantity { get; init; }

    /// <summary>
    /// 単価（注文時点の価格）
    /// </summary>
    public decimal UnitPrice { get; init; }
}
