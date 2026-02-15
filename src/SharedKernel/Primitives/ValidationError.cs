namespace SharedKernel.Primitives;

/// <summary>
/// 複数のバリデーションエラーを集約するエラーレコード
/// </summary>
/// <remarks>
/// FluentValidation の結果を一つのエラーにまとめるために使用します。
/// </remarks>
public sealed record ValidationError : Error
{
    /// <summary>
    /// 複数のバリデーションエラーから ValidationError を作成
    /// </summary>
    /// <param name="errors">個別のバリデーションエラーの配列</param>
    public ValidationError(Error[] errors)
        : base(
            "Validation.General",
            "One or more validation errors occurred",
            ErrorType.Validation)
    {
        Errors = errors;
    }

    /// <summary>個別のバリデーションエラーの配列</summary>
    public Error[] Errors { get; }

    /// <summary>
    /// Result のコレクションから失敗したもののエラーを集約
    /// </summary>
    /// <param name="results">Result オブジェクトのコレクション</param>
    /// <returns>失敗した Result のエラーを集約した ValidationError</returns>
    public static ValidationError FromResults(IEnumerable<Result> results) =>
        new(results.Where(r => r.IsFailure).Select(r => r.Error).ToArray()!);
}
