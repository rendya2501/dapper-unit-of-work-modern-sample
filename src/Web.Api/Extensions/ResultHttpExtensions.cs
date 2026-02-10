using Domain.Common.Results;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Infrastructure;

namespace Web.Api.Extensions;

/// <summary>
/// ResultをHTTPレスポンスに変換する拡張メソッド
/// </summary>
public static class ResultHttpExtensions
{
    /// <summary>
    /// 200 OK
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="result"></param>
    /// <returns></returns>
    public static IActionResult ToOk<T>(this Result<T> result) =>
        result.Match(
            value => new OkObjectResult(value),
            CustomResults.Problem);

    /// <summary>
    /// 201 Created At Action
    /// </summary>
    /// <param name="result"></param>
    /// <param name="actionName">アクション名（例: nameof(GetByIdAsync)）</param>
    /// <param name="controllerName">コントローラー名（例: "Orders"）。同一コントローラーなら null 可</param>
    /// <param name="routeValuesSelector">ルート値セレクター（例: id => new { id }）</param>
    /// <param name="responseSelector">レスポンスボディセレクター（例: id => new CreateResponse(id)）</param>
    public static IActionResult ToCreatedAtAction<T>(
        this Result<T> result,
        string actionName,
        string? controllerName,
        Func<T, object> routeValuesSelector,
        Func<T, object>? responseSelector = null) =>
            result.Match(
                value => new CreatedAtActionResult(
                    actionName,
                    controllerName,
                    routeValues: routeValuesSelector(value),
                    value: responseSelector is not null ? responseSelector(value) : value),
                CustomResults.Problem);

    /// <summary>
    /// 201 Created At Route
    /// </summary>
    public static IActionResult ToCreatedAtRoute<T>(
        this Result<T> result,
        string routeName,
        Func<T, object> routeValuesSelector,
        Func<T, object>? responseSelector = null) =>
            result.Match(
                value => new CreatedAtRouteResult(
                    routeName,
                    routeValuesSelector(value),
                    responseSelector is not null ? responseSelector(value) : value),
                CustomResults.Problem);

    /// <summary>
    /// 204 No Content（値なし Result）
    /// </summary>
    public static IActionResult ToNoContent(this Result result) =>
        result.Match(
            () => new NoContentResult(),
            CustomResults.Problem);

    /// <summary>
    /// カスタムレスポンス
    /// </summary>
    /// <typeparam name="T">返り値の型</typeparam>
    /// <param name="result">結果オブジェクト</param>
    /// <param name="onSuccess">成功時の処理</param>
    /// <returns>IResult オブジェクト</returns>
    /// <remarks>
    /// <para><strong>202 Accepted 例</strong></para>
    /// <code>
    /// public static async Task&lt;IResult&gt; Endpoint(...)
    ///     => (await sender.Send(new StartJobCommand(id), ct))
    ///         .ToResult(job => Results.Accepted($"/api/jobs/{job.Id}", job));
    /// </code>
    /// </remarks>
    public static IActionResult ToResult<T>(this Result<T> result, Func<T, IActionResult> onSuccess) =>
        result.Match(onSuccess, CustomResults.Problem);

    /// <summary>
    /// カスタムレスポンス
    /// </summary>
    /// <param name="result"></param>
    /// <param name="onSuccess"></param>
    /// <returns></returns>
    /// <remarks>
    /// <para><strong>複雑なロジック 例</strong></para>
    /// <code>
    /// public static async Task&lt;IResult&gt; Endpoint(ISender sender, int id, CancellationToken ct)
    ///     => (await sender.Send(new SomeCommand(id), ct))
    ///         .ToResult(value =>
    ///         {
    ///             // 複雑なロジック
    ///             var headers = new Dictionary&lt;string, string&gt;
    ///             {
    ///                 ["X-Custom-Header"] = value.SomeProperty
    ///             };
    ///             return Results.Ok(value);
    ///         });
    /// </code>
    /// </remarks>
    public static IActionResult ToResult(this Result result, Func<IActionResult> onSuccess) =>
        result.Match(onSuccess, CustomResults.Problem);
}
