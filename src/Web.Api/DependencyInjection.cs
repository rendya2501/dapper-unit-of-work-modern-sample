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
        // OpenAPI
        // ===================================================================
        services.AddEndpointsApiExplorer(); // APIエンドポイントの情報を探索可能にする
        services.AddOpenApi();


        // ===================================================================
        // 例外ハンドラー（順序が重要！）
        // ===================================================================
        // 特定の例外を先に登録
        services.AddExceptionHandler<ValidationExceptionHandler>();
        // グローバルハンドラーを最後に登録
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();


        return services;
    }
}
