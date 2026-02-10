using Application.Repositories;
using Domain.AuditLog;
using Domain.Common.Results;

namespace Application.Services;

/// <summary>
/// 監査ログサービスの実装
/// </summary>
public class AuditLogService(IAuditLogRepository repository)
{
    /// <inheritdoc />
    public async Task<Result<IEnumerable<AuditLog>>> GetAllAsync(
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var auditLogs = await repository.GetAllAsync(limit, cancellationToken);
        return Result.Success(auditLogs);
    }
}
