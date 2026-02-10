namespace Domain.Common.Results;

/// <summary>
/// エラー情報を表現する不変レコード
/// </summary>
/// <remarks>
/// Result パターンで使用され、例外を投げずに失敗を表現します。
/// </remarks>
public record Error
{
    /// <summary>
    /// エラーが存在しないことを表す特殊なインスタンス
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    /// <summary>
    /// Null値が提供された場合のエラー
    /// </summary>
    public static readonly Error NullValue = new(
        "General.Null",
        "Null value was provided",
        ErrorType.Failure);

    public Error(string code, string description, ErrorType type)
    {
        Code = code;
        Description = description;
        Type = type;
    }

    /// <summary>
    /// エラーコード（例: "User.NotFound", "Order.InvalidStatus"）
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// エラーの説明
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// エラーの種類（HTTP ステータスコードへのマッピングに使用）
    /// </summary>
    public ErrorType Type { get; }

    /// <summary>
    /// 一般的な失敗エラーを作成（HTTP 500）
    /// </summary>
    public static Error Failure(string code, string description) =>
        new(code, description, ErrorType.Failure);

    /// <summary>
    /// リソースが見つからないエラーを作成（HTTP 404）
    /// </summary>
    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    /// <summary>
    /// ビジネスルール違反エラーを作成（HTTP 400）
    /// </summary>
    /// <remarks>
    /// Validationとの違い: Validationは入力形式エラー、Problemはビジネスロジック違反
    /// </remarks>
    public static Error Problem(string code, string description) =>
        new(code, description, ErrorType.Problem);

    /// <summary>
    /// リソース競合エラーを作成（HTTP 409）
    /// </summary>
    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);
}
