using OrderManagement.Application.Contracts;
using OrderManagement.Application.Repositories;
using OrderManagement.Application.Services.Abstractions;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Services;

/// <summary>
/// 監査ログサービスの実装
/// </summary>
public class AuditLogService(IAuditLogRepository repository) : IAuditLogService
{
    /// <inheritdoc />
    public async Task<OperationResult<IEnumerable<AuditLog>>> GetAllAsync(int limit = 100)
    {
        var auditLogs = await repository.GetAllAsync(limit);
        return Result.Success(auditLogs);
    }
}
