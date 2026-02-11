using Domain.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Extensions;

/// <summary>
/// Result を RFC 7807 準拠の ProblemDetails に変換するヘルパークラス
/// </summary>
public static class CustomResults
{
    /// <summary>
    /// Result の失敗情報を ProblemDetails 形式の IActionResult に変換
    /// </summary>
    /// <param name="result">失敗した Result オブジェクト</param>
    /// <returns>ProblemDetails を含む IActionResult</returns>
    public static IActionResult Problem(Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException();
        }
        var status = GetStatusCode(result.Error.Type);

        var problemDetails = new ProblemDetails
        {
            Title = GetTitle(result.Error),
            Detail = GetDetail(result.Error),
            Type = GetType(result.Error.Type),
            Status = status
        };

        // ValidationError の場合、個別エラーを extensions に追加
        if (result.Error is ValidationError validationError)
        {
            problemDetails.Extensions["errors"] = validationError.Errors;
        }

        return new ObjectResult(problemDetails)
        {
            StatusCode = status
        };
    }

    // エラータイプに応じたタイトルを取得
    private static string GetTitle(Error error) =>
        error.Type switch
        {
            ErrorType.Validation => error.Code,
            ErrorType.Problem => error.Code,
            ErrorType.NotFound => error.Code,
            ErrorType.Conflict => error.Code,
            _ => "Server failure"
        };

    // エラータイプに応じた詳細説明を取得
    private static string GetDetail(Error error) =>
        error.Type switch
        {
            ErrorType.Validation => error.Description,
            ErrorType.Problem => error.Description,
            ErrorType.NotFound => error.Description,
            ErrorType.Conflict => error.Description,
            _ => "An unexpected error occurred"
        };

    // エラータイプに応じた RFC 7807 の type URI を取得
    private static string GetType(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            ErrorType.Problem => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            ErrorType.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            ErrorType.Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };

    // エラータイプに応じた HTTP ステータスコードを取得
    private static int GetStatusCode(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation or ErrorType.Problem => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
}
