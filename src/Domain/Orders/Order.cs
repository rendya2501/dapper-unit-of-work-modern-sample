using Domain.Common.Results;
using Domain.Inventory;

namespace Domain.Orders;

/// <summary>
/// 注文エンティティ（純粋ドメインモデル）
/// </summary>
/// <remarks>
/// <para><strong>設計原則</strong></para>
/// <list type="bullet">
/// <item>データベース構造の知識を持たない</item>
/// <item>Value Objectを使用（OrderId）</item>
/// <item>不変性を重視（privateセッター）</item>
/// <item>ビジネスルールをメソッドで表現</item>
/// <item>集約ルートとして整合性を保証</item>
/// </list>
/// 
/// <para><strong>Infrastructure層との関係</strong></para>
/// <para>
/// このクラスはDBテーブル構造を知りません。
/// Infrastructure層のMapperが永続化モデル（OrderRecord）との変換を担当します。
/// </para>
/// </remarks>
public class Order
{
    private readonly List<OrderDetail> _details = [];

    /// <summary>
    /// 注文ID（Value Object）
    /// </summary>
    /// <remarks>
    /// DB由来のIDですが、Value Objectでラップすることでドメインの概念として扱います。
    /// </remarks>
    public OrderId Id { get; private set; }

    /// <summary>
    /// 顧客ID
    /// </summary>
    /// <remarks>
    /// TODO: 将来的にはCustomerIdというValue Objectにすべき
    /// </remarks>
    public int CustomerId { get; private set; }

    /// <summary>
    /// 注文日時
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// 注文明細のコレクション（読み取り専用）
    /// </summary>
    /// <remarks>
    /// 外部から直接操作されないよう、読み取り専用リストとして公開。
    /// 明細の追加・削除はメソッド経由で行う。
    /// </remarks>
    public IReadOnlyList<OrderDetail> Details => _details.AsReadOnly();

    /// <summary>
    /// 注文合計金額（計算プロパティ）
    /// </summary>
    public decimal TotalAmount => _details.Sum(d => d.SubTotal);


    /// <summary>
    /// プライベートコンストラクタ（永続化層からの復元用）
    /// </summary>
    /// <remarks>
    /// Mapperからのみ呼び出されることを想定。
    /// 新規作成にはファクトリメソッド Create を使用。
    /// </remarks>
    private Order() { }


    /// <summary>
    /// 新しい注文を作成します（ファクトリメソッド）
    /// </summary>
    /// <param name="customerId">顧客ID</param>
    /// <returns>成功時は新規作成された注文、失敗時はエラー</returns>
    public static Result<Order> Create(int customerId)
    {
        if (customerId <= 0)
        {
            return Result.Failure<Order>(Error.Problem(
                "Order.InvalidCustomerId",
                "Customer ID must be greater than 0."));
        }

        return Result.Success(new Order
        {
            CustomerId = customerId,
            CreatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 注文明細を追加します
    /// </summary>
    /// <param name="productId">商品ID（Value Object）</param>
    /// <param name="quantity">数量</param>
    /// <param name="unitPrice">単価</param>
    /// <returns>成功または失敗</returns>
    public Result AddDetail(ProductId productId, int quantity, decimal unitPrice)
    {
        if (quantity < 1)
        {
            return Result.Failure(Error.Problem(
                "OrderDetail.InvalidQuantity",
                "Quantity must be at least 1."));
        }

        if (unitPrice <= 0)
        {
            return Result.Failure(Error.Problem(
                "OrderDetail.InvalidUnitPrice",
                "Unit price must be greater than 0."));
        }

        var detail = OrderDetail.Create(productId, quantity, unitPrice);
        _details.Add(detail);

        return Result.Success();
    }


    /// <summary>
    /// IDを設定します（永続化層からのみ呼び出し）
    /// </summary>
    /// <param name="id">注文ID</param>
    /// <remarks>
    /// このメソッドはinternalであり、Infrastructure層のMapperからのみ呼び出されます。
    /// ドメイン層からは呼び出せません。
    /// </remarks>
    internal void SetId(OrderId id) => Id = id;

    /// <summary>
    /// 作成日時を設定します（永続化層からのみ呼び出し）
    /// </summary>
    /// <param name="createdAt">作成日時</param>
    internal void SetCreatedAt(DateTime createdAt) => CreatedAt = createdAt;

    /// <summary>
    /// 明細リストを設定します（永続化層からのみ呼び出し）
    /// </summary>
    /// <param name="details">注文明細のリスト</param>
    /// <remarks>
    /// DB から復元する際に Mapper が使用します。
    /// </remarks>
    internal void SetDetails(IEnumerable<OrderDetail> details)
    {
        _details.Clear();
        _details.AddRange(details);
    }
}