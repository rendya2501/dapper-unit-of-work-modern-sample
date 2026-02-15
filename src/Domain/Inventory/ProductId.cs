namespace Domain.Inventory;

/// <summary>
/// 商品IDを表す値オブジェクト
/// </summary>
/// <remarks>
/// int型のProductIdを型安全に扱い、ドメインの概念を明確にします。
/// </remarks>
public readonly record struct ProductId
{
    /// <summary>
    /// ID値（内部表現）
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// ProductIdを作成します
    /// </summary>
    /// <param name="value">ID値</param>
    /// <exception cref="ArgumentException">値が1未満の場合</exception>
    public ProductId(int value)
    {
        if (value < 1)
        {
            throw new ArgumentException("Product ID must be greater than 0.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// int型からの暗黙的変換
    /// </summary>
    public static implicit operator ProductId(int value) => new(value);

    /// <summary>
    /// int型への明示的変換
    /// </summary>
    public static explicit operator int(ProductId productId) => productId.Value;

    /// <summary>
    /// 文字列表現
    /// </summary>
    public override string ToString() => Value.ToString();
}