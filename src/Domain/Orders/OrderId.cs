namespace Domain.Orders;

/// <summary>
/// 注文IDを表す値オブジェクト
/// </summary>
/// <remarks>
/// <para><strong>なぜValue Objectか</strong></para>
/// <list type="bullet">
/// <item>int型のIDをドメインの概念として型安全に扱う</item>
/// <item>プリミティブ型の氾濫を防ぐ（Primitive Obsession回避）</item>
/// <item>ドメイン層がDB実装詳細（AUTO_INCREMENTなど）から独立</item>
/// </list>
/// </remarks>
public readonly record struct OrderId
{
    /// <summary>
    /// ID値（内部表現）
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// OrderIdを作成します
    /// </summary>
    /// <param name="value">ID値</param>
    /// <exception cref="ArgumentException">値が1未満の場合</exception>
    public OrderId(int value)
    {
        if (value < 1)
        {
            throw new ArgumentException("Order ID must be greater than 0.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// int型からの暗黙的変換
    /// </summary>
    public static implicit operator OrderId(int value) => new(value);

    /// <summary>
    /// int型への明示的変換
    /// </summary>
    public static explicit operator int(OrderId orderId) => orderId.Value;

    /// <summary>
    /// 文字列表現
    /// </summary>
    public override string ToString() => Value.ToString();
}