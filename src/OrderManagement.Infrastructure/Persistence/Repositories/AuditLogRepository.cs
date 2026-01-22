using Dapper;
using OrderManagement.Application.Common;
using OrderManagement.Application.Repositories;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// 監査ログリポジトリの実装
/// </summary>
public class AuditLogRepository(IDbSession session)
    : IAuditLogRepository
{
    /// <inheritdoc />
    public async Task CreateAsync(AuditLog log)
    {
        const string sql = """
            INSERT INTO AuditLog (Action, Details, CreatedAt)
            VALUES (@Action, @Details, @CreatedAt)
            """;

        await session.Connection.ExecuteAsync(sql, log, session.Transaction);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLog>> GetAllAsync(int limit = 100)
    {
        const string sql = """
            SELECT * FROM AuditLog 
            ORDER BY CreatedAt DESC 
            LIMIT @Limit
            """;

        return await session.Connection.QueryAsync<AuditLog>(
            sql, new { Limit = limit }, session.Transaction);
    }
}
