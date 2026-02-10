using Domain.Common.Results;

namespace Web.Api.Extensions;

/// <summary>
/// Resultをマッチングする拡張メソッド
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Resultをマッチングする（値なし）
    /// </summary>
    /// <typeparam name="TOut">成功時の戻り値の型</typeparam>
    /// <param name="result">Result</param>
    /// <param name="onSuccess">成功時の処理</param>
    /// <param name="onFailure">失敗時の処理</param>
    /// <returns>成功時の戻り値</returns>
    public static TOut Match<TOut>(
        this Result result,
        Func<TOut> onSuccess,
        Func<Result, TOut> onFailure)
    {
        return result.IsSuccess
            ? onSuccess()
            : onFailure(result);
    }

    /// <summary>
    /// Resultをマッチングする（値あり）
    /// </summary>
    /// <typeparam name="TIn">入力の型</typeparam>
    /// <typeparam name="TOut">成功時の戻り値の型</typeparam>
    /// <param name="result">Result</param>
    /// <param name="onSuccess">成功時の処理</param>
    /// <param name="onFailure">失敗時の処理</param>
    /// <returns>成功時の戻り値</returns>
    public static TOut Match<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> onSuccess,
        Func<Result<TIn>, TOut> onFailure)
    {
        return result.IsSuccess
            ? onSuccess(result.Value)
            : onFailure(result);
    }
}
