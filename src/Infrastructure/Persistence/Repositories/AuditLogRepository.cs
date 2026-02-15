using Application.Common;
using Application.Repositories;
using Dapper;
using SharedKernel.Models;

namespace Infrastructure.Persistence.Repositories;

/// <summary>
/// 監査ログリポジトリの実装
/// </summary>
public class AuditLogRepository(IDbSession session)
    : IAuditLogRepository
{
    /// <inheritdoc />
    public async Task CreateAsync(
        AuditLogRecord record, 
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO AuditLog (Action, Details, CreatedAt)
            VALUES (@Action, @Details, @CreatedAt)
            """;

        var command = new CommandDefinition(
            sql,
            record,
            session.Transaction,
            cancellationToken: cancellationToken);

        await session.Connection.ExecuteAsync(command);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLogRecord>> GetAllAsync(
        int limit = 100, 
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT * FROM AuditLog 
            ORDER BY CreatedAt DESC 
            LIMIT @Limit
            """;

        var command = new CommandDefinition(
            sql,
            new { Limit = limit },
            session.Transaction,
            cancellationToken: cancellationToken);

        return await session.Connection.QueryAsync<AuditLogRecord>(command);
    }
}
