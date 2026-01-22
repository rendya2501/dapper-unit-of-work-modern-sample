namespace OrderManagement.Application.Contracts;


public abstract record OperationResultBase
{
    public sealed record NotFound(string? Message = null) : OperationResultBase;
    public sealed record ValidationError(Dictionary<string, string[]> Errors) : OperationResultBase;
    public sealed record Conflict(string Message) : OperationResultBase;
    public sealed record BusinessRuleValidation(string Message) : OperationResultBase;
}

/// <summary>
/// 改善版：Success() と Success(T) を両方サポート
/// </summary>
public abstract record OperationResult<T>
{
    private OperationResult() { }

    public sealed record Success(T Value) : OperationResult<T>;
    public sealed record SuccessEmpty : OperationResult<T>;  // ← Unit不要
    private sealed record Error(OperationResultBase Base) : OperationResult<T>;

    public static implicit operator OperationResult<T>(OperationResultBase error)
        => new Error(error);

    // ★ 改善：onError を自動でハンドリングするオーバーロード
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<TResult> onSuccessEmpty,
        Func<OperationResultBase, TResult> onError)
    {
        return this switch
        {
            Success s => onSuccess(s.Value),
            SuccessEmpty => onSuccessEmpty(),
            Error e => onError(e.Base),
            _ => throw new InvalidOperationException()
        };
    }
}

/// <summary>
/// 値を返さない操作専用の Result
/// </summary>
public abstract record OperationResult
{
    private OperationResult() { }

    public sealed record Success : OperationResult;  // ← T不要
    private sealed record Error(OperationResultBase Base) : OperationResult;

    public static implicit operator OperationResult(OperationResultBase error)
        => new Error(error);

    public TResult Match<TResult>(
        Func<TResult> onSuccess,
        Func<OperationResultBase, TResult> onError)
    {
        return this switch
        {
            Success => onSuccess(),
            Error e => onError(e.Base),
            _ => throw new InvalidOperationException()
        };
    }
}


// ===================================
// Factory（改善版）
// ===================================
public static class Result
{
    // 値を返す Success
    public static OperationResult<T> Success<T>(T value)
        => new OperationResult<T>.Success(value);

    // 値を返さない Success（Unit不要）
    public static OperationResult Success()
        => new OperationResult.Success();

    // エラー系
    public static OperationResultBase NotFound(string? message = null)
        => new OperationResultBase.NotFound(message);

    public static OperationResultBase ValidationError(Dictionary<string, string[]> errors)
        => new OperationResultBase.ValidationError(errors);

    public static OperationResultBase ValidationError(string field, string error)
        => new OperationResultBase.ValidationError(
            new Dictionary<string, string[]> { [field] = [error] });

    public static OperationResultBase Conflict(string message)
        => new OperationResultBase.Conflict(message);

    public static OperationResultBase BusinessRuleValidation(string message)
        => new OperationResultBase.BusinessRuleValidation(message);
}




//public abstract record EndpointResultBase
//{
//    public sealed record NotFound(string Message) : EndpointResultBase;

//    public sealed record ValidationError(string Message) : EndpointResultBase;

//    public sealed record Conflict(string Message) : EndpointResultBase;
//}


//public abstract record EndpointResult<T>
//{
//    private EndpointResult() { }

//    public sealed record Ok(T Value) : EndpointResult<T>;
//    public sealed record Created(T Value, string ActionName, object RouteValues) : EndpointResult<T>;
//    public sealed record NoContent : EndpointResult<T>;

//    private sealed record Error(EndpointResultBase Base) : EndpointResult<T>;

//    public static implicit operator EndpointResult<T>(EndpointResultBase error)
//        => new Error(error);

//    public TResult Match<TResult>(
//        Func<T, TResult> onOk,
//        Func<T, string, object, TResult> onCreated,
//        Func<TResult> onNoContent,
//        Func<EndpointResultBase, TResult> onError)
//    {
//        return this switch
//        {
//            Ok o => onOk(o.Value),
//            Created c => onCreated(c.Value, c.ActionName, c.RouteValues),
//            NoContent => onNoContent(),
//            Error e => onError(e.Base),
//            _ => throw new InvalidOperationException()
//        };
//    }
//}

//public static class EndpointResult
//{
//    public static EndpointResult<T> Ok<T>(T value)
//        => new EndpointResult<T>.Ok(value);

//    public static EndpointResult<T> Created<T>(
//        T value,
//        string actionName,
//        object routeValues)
//        => new EndpointResult<T>.Created(value, actionName, routeValues);

//    public static EndpointResult<T> NoContent<T>()
//        => new EndpointResult<T>.NoContent();

//    public static EndpointResultBase NotFound(string message)
//        => new EndpointResultBase.NotFound(message);

//    public static EndpointResultBase ValidationError(string message)
//        => new EndpointResultBase.ValidationError(message);
//}
