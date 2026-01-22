namespace OrderManagement.Infrastructure.Persistence.UnitOfWork.UnionType;

/// <summary>
/// Unit of Work 実行結果を表すユニオン型
/// </summary>
/// <typeparam name="T">成功時の戻り値の型</typeparam>
public abstract record UnitOfWorkResult<T>
{
    /// <summary>
    /// 成功（Commit）
    /// </summary>
    public sealed record Success(T Value) : UnitOfWorkResult<T>;

    /// <summary>
    /// 中断（Rollback）- エラーメッセージ付き
    /// </summary>
    public sealed record Abort(string Reason) : UnitOfWorkResult<T>;

    /// <summary>
    /// 成功結果を生成
    /// </summary>
    public static UnitOfWorkResult<T> Ok(T value) => new Success(value);

    /// <summary>
    /// 中断結果を生成
    /// </summary>
    public static UnitOfWorkResult<T> Cancel(string reason) => new Abort(reason);

    /// <summary>
    /// パターンマッチングによる値の取り出し
    /// </summary>
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<string, TResult> onAbort)
    {
        return this switch
        {
            Success s => onSuccess(s.Value),
            Abort a => onAbort(a.Reason),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }
}
