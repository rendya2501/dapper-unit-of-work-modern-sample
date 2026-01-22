using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Repositories;

/// <summary>
/// 在庫リポジトリのインターフェース
/// </summary>
public interface IInventoryRepository
{
    /// <summary>
    /// 商品IDを指定して在庫を取得します
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <returns>在庫情報。見つからない場合は null</returns>
    Task<Inventory?> GetByProductIdAsync(int productId);

    /// <summary>
    /// すべての在庫を取得します
    /// </summary>
    /// <returns>在庫のリスト</returns>
    Task<IEnumerable<Inventory>> GetAllAsync();

    /// <summary>
    /// 在庫を新規作成します
    /// </summary>
    /// <param name="inventory"></param>
    /// <returns></returns>
    Task<int> CreateAsync(Inventory inventory);

    /// <summary>
    /// 在庫数を更新します
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="newStock">新しい在庫数</param>
    /// <returns>更新された行数</returns>
    Task<int> UpdateStockAsync(int productId, int newStock);

    /// <summary>
    /// 在庫を更新します
    /// </summary>
    /// <param name="productId"></param>
    /// <param name="productName"></param>
    /// <param name="stock"></param>
    /// <param name="unitPrice"></param>
    /// <returns></returns>
    Task UpdateAsync(int productId, string productName, int stock, decimal unitPrice);

    /// <summary>
    /// 在庫を削除します
    /// </summary>
    /// <param name="productId"></param>
    /// <returns></returns>
    Task DeleteAsync(int productId);
}
