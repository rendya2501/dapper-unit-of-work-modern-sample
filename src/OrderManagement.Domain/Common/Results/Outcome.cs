namespace OrderManagement.Domain.Common.Results;

/// <summary>
/// 操作結果を生成するファクトリ
/// 
/// 命名理由：
/// - "Outcome" = 操作の「結果・成果」を表す自然な英語
/// - Result より具体的で、戻り値の Result 型と区別できる
/// - Try, Succeed, Fail などの動詞と相性が良い
/// </summary>
public static class Outcome
{
    // === 成功 ===

    public static OperationResult<T> Success<T>(T value)
        => new OperationResult<T>.Success(value);

    public static OperationResult Success()
        => new OperationResult.Success();

    // === リソース系エラー ===

    public static OperationError NotFound(string? message = null)
        => new OperationError.NotFound(message);

    // === バリデーション系エラー ===

    public static OperationError ValidationFailed(Dictionary<string, string[]> errors)
        => new OperationError.ValidationFailed(errors);

    public static OperationError ValidationFailed(string field, string error)
        => new OperationError.ValidationFailed(
            new Dictionary<string, string[]> { [field] = [error] });

    // === ビジネスルール違反エラー ===

    /// <summary>
    /// リソースの状態が操作を許可しない
    /// 
    /// 使用例：
    /// - ユーザーが既に存在する
    /// - 注文が既にキャンセルされている
    /// - 在庫がロックされている
    /// </summary>
    public static OperationError Conflict(string message)
        => new OperationError.Conflict(message);

    /// <summary>
    /// ビジネスルールに違反している
    /// 
    /// 使用例：
    /// - 在庫不足（INSUFFICIENT_STOCK）
    /// - 注文数が0（INVALID_QUANTITY）
    /// - 期限切れ（EXPIRED）
    /// - 条件未達（CONDITION_NOT_MET）
    /// 
    /// Code は定数化を推奨
    /// </summary>
    public static OperationError BusinessRule(string code, string message)
        => new OperationError.BusinessRule(code, message);

    // === 権限系エラー ===

    public static OperationError Unauthorized(string message = "Authentication required")
        => new OperationError.Unauthorized(message);

    public static OperationError Forbidden(string message = "Insufficient permissions")
        => new OperationError.Forbidden(message);
}
