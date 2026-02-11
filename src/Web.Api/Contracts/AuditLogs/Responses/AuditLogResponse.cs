namespace Web.Api.Contracts.AuditLogs.Responses;

/// <summary>
/// 
/// </summary>
/// <param name="Id">ログID</param>
/// <param name="Action">アクション種別</param>
/// <param name="Details">詳細情報</param>
/// <param name="CreatedAt">記録日時</param>
public record AuditLogResponse(
    int Id,
    string Action,
    string Details,
    DateTime CreatedAt);
