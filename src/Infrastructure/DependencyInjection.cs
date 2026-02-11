using Application.Common;
using Application.Repositories;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Data;

namespace Infrastructure;

/// <summary>
/// Infrastructure レイヤーの依存性注入設定
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Infrastructure レイヤーの依存性を登録します。
    /// </summary>
    /// <remarks>
    /// <para>以下のコンポーネントを登録します。</para>
    /// <list type="bullet">
    /// <item>データベース接続（<see cref="IDbConnection"/>）</item>
    /// <item>データベースセッション（<see cref="DbSession"/>）</item>
    /// <item>Unit of Work（<see cref="IUnitOfWork"/>）</item>
    /// <item>各リポジトリ実装</item>
    /// </list>
    /// <para>
    /// すべてのコンポーネントは Scoped ライフタイムで登録されます。
    /// 1つのHTTPリクエスト内で同一インスタンスが共有されます。
    /// </para>
    /// </remarks>
    /// <param name="services">サービスコレクション</param>
    /// <param name="configuration">アプリケーション設定（接続文字列の取得に使用）</param>
    /// <returns>メソッドチェーン用の <see cref="IServiceCollection"/></returns>
    /// <exception cref="InvalidOperationException">接続文字列 "DefaultConnection" が設定されていない場合</exception>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' not found.");

        // DB接続
        // 先に IDbConnection を登録することで、後続の DbSession で再利用可能にする
        services.AddScoped<IDbConnection>(sp => new SqliteConnection(connectionString));

        // DbSession（具象クラスを1回だけ登録）
        services.AddScoped<DbSession>();
        // IDbSessionManager (UnitOfWork用)
        services.AddScoped<IDbSessionManager>(sp => sp.GetRequiredService<DbSession>());
        // IDbSession (リポジトリ用)
        services.AddScoped<IDbSession>(sp => sp.GetRequiredService<DbSession>());

        // UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();

        return services;
    }
}
