namespace OrderManagement.Domain.Common.Results;

/// <summary>
/// 操作が失敗した理由を表す型
/// ビジネスエラー・技術エラーの両方を含む
/// </summary>
public abstract record OperationError
{
    // === リソース系エラー ===

    /// <summary>
    /// 指定されたリソースが見つからない
    /// </summary>
    public sealed record NotFound(string? Message = null) : OperationError;

    // === バリデーション系エラー ===

    /// <summary>
    /// 入力データの形式・内容が不正
    /// </summary>
    public sealed record ValidationFailed(Dictionary<string, string[]> Errors) : OperationError;

    // === ビジネスルール違反エラー ===

    /// <summary>
    /// リソースの状態が操作を許可しない
    /// 例：既に存在する、重複している、状態遷移できない
    /// </summary>
    public sealed record Conflict(string Message) : OperationError;

    /// <summary>
    /// ビジネスルールに違反している
    /// 例：在庫不足、数量が0、期限切れ、条件未達
    /// </summary>
    public sealed record BusinessRule(string Code, string Message) : OperationError;

    // === 権限系エラー ===

    /// <summary>
    /// 認証が必要
    /// </summary>
    public sealed record Unauthorized(string Message = "Authentication required") : OperationError;

    /// <summary>
    /// 権限がない
    /// </summary>
    public sealed record Forbidden(string Message = "Insufficient permissions") : OperationError;
}
