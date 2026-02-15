namespace SharedKernel.Primitives;

/// <summary>
/// エラーの分類（HTTP ステータスコードへのマッピングに使用）
/// </summary>
public enum ErrorType
{
    /// <summary>システムエラー（HTTP 500）</summary>
    Failure = 0,

    /// <summary>入力データの検証エラー（HTTP 400）</summary>
    Validation = 1,

    /// <summary>ビジネスルール違反（HTTP 400）</summary>
    Problem = 2,

    /// <summary>リソースが見つからない（HTTP 404）</summary>
    NotFound = 3,

    /// <summary>リソース競合（HTTP 409）</summary>
    Conflict = 4
}
