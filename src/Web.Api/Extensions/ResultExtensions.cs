using SharedKernel.Primitives;

namespace Web.Api.Extensions;

/// <summary>
/// <see cref="Result"/> および <see cref="Result{TValue}"/> のパターンマッチング拡張メソッド
/// </summary>
/// <remarks>
/// <para>
/// 成功・失敗の分岐を表現するための基盤メソッドです。
/// HTTP レスポンスへの変換には <see cref="ResultHttpExtensions"/> を使用してください。
/// </para>
/// </remarks>
public static class ResultExtensions
{
    /// <summary>
    /// 成功・失敗に応じて処理を分岐します（値なし版）
    /// </summary>
    /// <typeparam name="TOut">戻り値の型</typeparam>
    /// <param name="result">分岐対象の Result</param>
    /// <param name="onSuccess">成功時に実行する処理</param>
    /// <param name="onFailure">失敗時に実行する処理。<see cref="Result"/> を受け取りエラー情報にアクセスできます</param>
    /// <returns>成功・失敗それぞれの処理結果</returns>
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
    /// 成功・失敗に応じて処理を分岐します（値あり版）
    /// </summary>
    /// <typeparam name="TIn">Result が保持する値の型</typeparam>
    /// <typeparam name="TOut">戻り値の型</typeparam>
    /// <param name="result">分岐対象の Result</param>
    /// <param name="onSuccess">成功時に実行する処理。成功値を受け取ります</param>
    /// <param name="onFailure">失敗時に実行する処理。<see cref="Result{TValue}"/> を受け取りエラー情報にアクセスできます</param>
    /// <returns>成功・失敗それぞれの処理結果</returns>
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
