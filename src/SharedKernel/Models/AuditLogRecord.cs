namespace Shared.Models;

/// <summary>
/// 監査ログレコード（Shared Kernel）
/// </summary>
/// <remarks>
/// <para><strong>なぜShared Kernelか</strong></para>
/// <para>
/// 監査ログは：<br/>
/// - ビジネスロジックを持たない（読み取り専用）<br/>
/// - 全層で同じ構造を使用<br/>
/// - ドメインモデルとして扱う必要がない<br/>
/// </para>
/// <para>
/// このような「データ構造のみ」のモデルはShared Kernelとして<br/>
/// 全層から参照可能にするのがDDDのベストプラクティスです。
/// </para>
/// 
/// <para><strong>Shared Kernelとは</strong></para>
/// <para>
/// Eric Evans（DDD提唱者）が定義した概念で、<br/>
/// 複数の境界づけられたコンテキスト（Bounded Context）間で<br/>
/// 共有される共通モデルを指します。
/// </para>
/// 
/// <para><strong>使用箇所</strong></para>
/// <list type="bullet">
/// <item>Application: IAuditLogRepository</item>
/// <item>Infrastructure: AuditLogRepository実装</item>
/// <item>Web.Api: Response DTO（必要なら）</item>
/// </list>
/// </remarks>
public record AuditLogRecord
{
    /// <summary>
    /// ログID（主キー、AUTO_INCREMENT）
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// アクション種別（例: "ORDER_CREATED", "INVENTORY_UPDATED"）
    /// </summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>
    /// 詳細情報
    /// </summary>
    public string Details { get; init; } = string.Empty;

    /// <summary>
    /// 記録日時（UTC）
    /// </summary>
    public DateTime CreatedAt { get; init; }
}
