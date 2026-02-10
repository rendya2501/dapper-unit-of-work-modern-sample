using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.ExceptionHandlers;

/// <summary>
/// ValidationException専用のハンドラー
/// </summary>
/// <remarks>
/// FluentValidationの例外をRFC 7807準拠のProblemDetailsに変換
/// </remarks>
internal sealed class ValidationExceptionHandler(
    ILogger<ValidationExceptionHandler> logger,
    IHostEnvironment environment)
        : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        logger.LogWarning(
            validationException,
            "Validation failed for {RequestPath}",
            httpContext.Request.Path);

        // RFC 7807準拠のProblemDetailsレスポンスを作成
        var problemDetails = new ValidationProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Instance = httpContext.Request.Path,
            Errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray())
        };

        // 開発環境でのみ詳細情報を追加
        if (environment.IsDevelopment())
        {
            problemDetails.Extensions["validationDetails"] = validationException.Errors
                .Select(e => new
                {
                    e.PropertyName,
                    e.ErrorMessage,
                    e.ErrorCode,
                    AttemptedValue = e.AttemptedValue?.ToString(),
                    e.Severity
                })
                .ToArray();
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true; // 例外を処理済み
    }
}
