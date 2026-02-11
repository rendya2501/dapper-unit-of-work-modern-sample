using Domain.AuditLog;

namespace Web.Api.Contracts.AuditLogs.Responses;

public static class AuditLogMappingExtensions
{
    public static AuditLogResponse ToResponse(this AuditLog log) =>
        new(
            log.Id,
            log.Action,
            log.Details,
            log.CreatedAt);
}
