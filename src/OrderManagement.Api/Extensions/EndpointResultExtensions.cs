using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.Contracts.Responses;
using OrderManagement.Application.Contracts;
using System.Net;

namespace OrderManagement.Api.Extensions;

/// <summary>
/// ★ 改善：onError を自動処理する拡張メソッド
/// </summary>
public static class ServiceResultExtensions
{
    /// <summary>
    /// 値を返す Result 用の自動変換
    /// エラーハンドリングは自動
    /// </summary>
    public static IActionResult ToActionResult<T>(
        this OperationResult<T> result,
        ControllerBase controller,
        Func<T, IActionResult> onSuccess)
    {
        return result.Match(
            onSuccess: onSuccess,
            onSuccessEmpty: () => controller.NoContent(),
            onError: error => HandleError(controller, error));
    }

    /// <summary>
    /// 値を返さない Result 用の自動変換
    /// </summary>
    public static IActionResult ToActionResult(
        this OperationResult result,
        ControllerBase controller,
        Func<IActionResult>? onSuccess = null)
    {
        return result.Match(
            onSuccess: onSuccess ?? (() => controller.NoContent()),
            onError: error => HandleError(controller, error));
    }

    /// <summary>
    /// 共通エラーハンドリング（private）
    /// </summary>
    private static IActionResult HandleError(
        ControllerBase controller,
        OperationResultBase error)
    {
        return error switch
        {
            OperationResultBase.NotFound nf => controller.NotFound(
                new ErrorResponse(nf.Message ?? "Resource not found")),

            OperationResultBase.ValidationError ve => controller.BadRequest(
                new ErrorResponse("Validation failed", ve.Errors)),

            OperationResultBase.Conflict c => controller.Conflict(
                new ErrorResponse(c.Message)),

            _ => controller.StatusCode((int)HttpStatusCode.InternalServerError,
                new ErrorResponse("Internal server error"))
        };
    }
}