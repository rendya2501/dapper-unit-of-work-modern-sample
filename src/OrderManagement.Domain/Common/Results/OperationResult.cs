namespace OrderManagement.Domain.Common.Results;

/// <summary>
/// 値を返す操作の結果
/// 成功時は T 型の値を持つ
/// </summary>
public abstract record OperationResult<T>
{
    private OperationResult() { }

    public sealed record Success(T Value) : OperationResult<T>;
    public sealed record SuccessEmpty : OperationResult<T>;
    private sealed record Failure(OperationError Error) : OperationResult<T>;

    // エラーからの暗黙的変換
    public static implicit operator OperationResult<T>(OperationError error)
        => new Failure(error);

    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<TResult> onSuccessEmpty,
        Func<OperationError, TResult> onFailure)
    {
        return this switch
        {
            Success s => onSuccess(s.Value),
            SuccessEmpty => onSuccessEmpty(),
            Failure f => onFailure(f.Error),
            _ => throw new InvalidOperationException()
        };
    }


    public bool IsSuccess => this is Success or SuccessEmpty;
    public bool IsFailure => this is Failure;

    public OperationError? Error => this is Failure f ? f.Error : null;

    public string ErrorMessage => Error switch
    {
        OperationError.NotFound nf => nf.Message ?? "Resource not found",
        OperationError.ValidationFailed vf => $"Validation failed: {string.Join(", ", vf.Errors.Keys)}",
        OperationError.Conflict c => c.Message,
        OperationError.BusinessRule br => $"[{br.Code}] {br.Message}",
        OperationError.Unauthorized u => u.Message,
        OperationError.Forbidden f => f.Message,
        _ => "Unknown error"
    };
}


/// <summary>
/// 値を返さない操作の結果
/// 成功 or 失敗のみを表現
/// </summary>
public abstract record OperationResult
{
    private OperationResult() { }

    public sealed record Success : OperationResult;
    private sealed record Failure(OperationError Error) : OperationResult;

    public static implicit operator OperationResult(OperationError error)
        => new Failure(error);

    public TResult Match<TResult>(
        Func<TResult> onSuccess,
        Func<OperationError, TResult> onFailure)
    {
        return this switch
        {
            Success => onSuccess(),
            Failure f => onFailure(f.Error),
            _ => throw new InvalidOperationException()
        };
    }

    public bool IsSuccess => this is Success;
    public bool IsFailure => this is Failure;

    public OperationError? Error => this is Failure f ? f.Error : null;

    public string ErrorMessage => Error switch
    {
        OperationError.NotFound nf => nf.Message ?? "Resource not found",
        OperationError.ValidationFailed vf => $"Validation failed: {string.Join(", ", vf.Errors.Keys)}",
        OperationError.Conflict c => c.Message,
        OperationError.BusinessRule br => $"[{br.Code}] {br.Message}",
        OperationError.Unauthorized u => u.Message,
        OperationError.Forbidden f => f.Message,
        _ => "Unknown error"
    };
}
