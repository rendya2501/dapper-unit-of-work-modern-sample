using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.Contracts.Responses;
using OrderManagement.Domain.Common.Results;

namespace OrderManagement.Api.Extensions;


public static class OperationResultExtensions
{
    public static IActionResult ToActionResult<T>(
        this OperationResult<T> result,
        ControllerBase controller,
        Func<T, IActionResult> onSuccess)
    {
        return result.Match(
            onSuccess: onSuccess,
            onSuccessEmpty: () => controller.NoContent(),
            onFailure: faiure => HandleError(controller, faiure));
    }

    public static IActionResult ToActionResult(
        this OperationResult result,
        ControllerBase controller,
        Func<IActionResult>? onSuccess = null)
    {
        return result.Match(
            onSuccess: onSuccess ?? (() => controller.NoContent()),
            onFailure: faiure => HandleError(controller, faiure));
    }

    private static IActionResult HandleError(
        ControllerBase controller,
        OperationError error)
    {
        return error switch
        {
            OperationError.NotFound nf => controller.NotFound(
                new ErrorResponse(nf.Message ?? "Resource not found")),

            OperationError.ValidationFailed vf => controller.BadRequest(
                new ErrorResponse("Validation failed", vf.Errors)),

            OperationError.Conflict c => controller.Conflict(
                new ErrorResponse(c.Message)),

            OperationError.BusinessRule br => controller.BadRequest(
                new BusinessErrorResponse(br.Code, br.Message)),

            OperationError.Unauthorized _ => controller.Unauthorized(),

            OperationError.Forbidden f => controller.StatusCode(403,
                new ErrorResponse(f.Message)),

            _ => controller.StatusCode(500,
                new ErrorResponse("Internal server error"))
        };
    }
}
