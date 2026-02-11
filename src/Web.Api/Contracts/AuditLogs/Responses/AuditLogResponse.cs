namespace Web.Api.Contracts.AuditLogs.Responses;

/// <summary>
/// 監査ログのレスポンスDTO
/// </summary>
/// <param name="Id">ログID</param>
/// <param name="Action">アクション種別（例: "ORDER_CREATED", "INVENTORY_UPDATED"）</param>
/// <param name="Details">操作の詳細情報</param>
/// <param name="CreatedAt">ログの記録日時（UTC）</param>
public record AuditLogResponse(
    int Id,
    string Action,
    string Details,
    DateTime CreatedAt);
