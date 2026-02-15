using SharedKernel.Primitives;

namespace Domain.Inventory;

/// <summary>
/// 在庫エンティティ（純粋ドメインモデル）
/// </summary>
/// <remarks>
/// <para><strong>設計変更のポイント</strong></para>
/// <list type="bullet">
/// <item>以前: publicセッターのデータクラス</item>
/// <item>今: privateセッター + ドメインロジック</item>
/// <item>在庫の増減をメソッドで表現</item>
/// <item>不変条件（在庫はマイナスにならない）を保証</item>
/// </list>
/// </remarks>
public class Inventory
{
    /// <summary>
    /// 商品ID（Value Object）
    /// </summary>
    public ProductId ProductId { get; private set; }

    /// <summary>
    /// 商品名
    /// </summary>
    public string ProductName { get; private set; } = string.Empty;

    /// <summary>
    /// 在庫数
    /// </summary>
    public int Stock { get; private set; }

    /// <summary>
    /// 単価
    /// </summary>
    public decimal UnitPrice { get; private set; }


    /// <summary>
    /// プライベートコンストラクタ
    /// </summary>
    private Inventory() { }


    /// <summary>
    /// 新しい在庫を作成します（ファクトリメソッド）
    /// </summary>
    /// <param name="productName">商品名</param>
    /// <param name="stock">初期在庫数</param>
    /// <param name="unitPrice">単価</param>
    /// <returns>成功時は新規作成された在庫、失敗時はエラー</returns>
    public static Result<Inventory> Create(string productName, int stock, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return Result.Failure<Inventory>(Error.Problem(
                "Inventory.InvalidProductName",
                "Product name cannot be empty."));
        }

        if (stock < 0)
        {
            return Result.Failure<Inventory>(Error.Problem(
                "Inventory.NegativeStock",
                "Stock cannot be negative."));
        }

        if (unitPrice <= 0)
        {
            return Result.Failure<Inventory>(Error.Problem(
                "Inventory.InvalidUnitPrice",
                "Unit price must be greater than 0."));
        }

        return Result.Success(new Inventory
        {
            ProductName = productName,
            Stock = stock,
            UnitPrice = unitPrice
        });
    }

    /// <summary>
    /// 在庫を減算します
    /// </summary>
    /// <param name="quantity">減算する数量</param>
    /// <returns>成功または失敗</returns>
    /// <remarks>
    /// ドメインロジック: 在庫はマイナスにならない
    /// </remarks>
    public Result Decrease(int quantity)
    {
        if (quantity < 1)
        {
            return Result.Failure(Error.Problem(
                 "Inventory.InvalidQuantity",
                 "Quantity must be at least 1."));
        }

        if (Stock < quantity)
        {
            return Result.Failure(Error.Problem(
                "Inventory.InsufficientStock",
                $"Insufficient stock. Available: {Stock}, Requested: {quantity}"));
        }

        Stock -= quantity;
        return Result.Success();
    }

    /// <summary>
    /// 在庫を増加します
    /// </summary>
    /// <param name="quantity">増加する数量</param>
    /// <returns>成功または失敗</returns>
    public Result Increase(int quantity)
    {
        if (quantity < 1)
        {
            return Result.Failure(Error.Problem(
               "Inventory.InvalidQuantity",
               "Quantity must be at least 1."));
        }

        Stock += quantity;
        return Result.Success();
    }

    /// <summary>
    /// 在庫情報を更新します
    /// </summary>
    /// <param name="productName">新しい商品名</param>
    /// <param name="stock">新しい在庫数</param>
    /// <param name="unitPrice">新しい単価</param>
    /// <returns>成功または失敗</returns>
    public Result Update(string productName, int stock, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return Result.Failure(Error.Problem(
                "Inventory.InvalidProductName",
                "Product name cannot be empty."));
        }

        if (stock < 0)
        {
            return Result.Failure(Error.Problem(
                "Inventory.NegativeStock",
                "Stock cannot be negative."));
        }

        if (unitPrice <= 0)
        {
            return Result.Failure(Error.Problem(
                "Inventory.InvalidUnitPrice",
                "Unit price must be greater than 0."));
        }

        ProductName = productName;
        Stock = stock;
        UnitPrice = unitPrice;
        return Result.Success();
    }

    /// <summary>
    /// IDを設定します（永続化層からのみ呼び出し）
    /// </summary>
    /// <param name="id">商品ID</param>
    internal void SetId(ProductId id) => ProductId = id;

    /// <summary>
    /// 永続化層からの復元用ファクトリメソッド
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="productName">商品名</param>
    /// <param name="stock">在庫数</param>
    /// <param name="unitPrice">単価</param>
    /// <returns>復元された在庫エンティティ</returns>
    internal static Inventory Restore(ProductId productId, string productName, int stock, decimal unitPrice)
    {
        return new Inventory
        {
            ProductId = productId,
            ProductName = productName,
            Stock = stock,
            UnitPrice = unitPrice
        };
    }
}
