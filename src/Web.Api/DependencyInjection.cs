using FluentValidation;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using Web.Api.ExceptionHandlers;

namespace Web.Api;

/// <summary>
/// 依存性注入の設定クラス
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Web.Api の依存性注入の設定
    /// </summary>
    /// <param name="services">IServiceCollection</param>
    /// <returns>IServiceCollection</returns>
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        // ===================================================================
        // Controllers
        // ===================================================================
        services.AddControllers();


        // ===================================================================
        // FluentValidation
        // ===================================================================
        services.AddValidatorsFromAssemblyContaining<Program>();
        services.AddFluentValidationAutoValidation();


        // ===================================================================
        // OpenAPI
        // ===================================================================
        services.AddEndpointsApiExplorer(); // APIエンドポイントの情報を探索可能にする
        services.AddOpenApi();


        // ===================================================================
        // 例外ハンドラー（順序が重要！）
        // ===================================================================
        services.AddExceptionHandler<ValidationExceptionHandler>();// 特定の例外を先に登録
        services.AddExceptionHandler<GlobalExceptionHandler>();// グローバルハンドラーを最後に登録
        services.AddProblemDetails();


        return services;
    }
}
