namespace Infrastructure.Persistence.Models;

/// <summary>
/// Orders テーブルの永続化契約
/// </summary>
/// <remarks>
/// <para><strong>目的</strong></para>
/// <para>
/// Application ⇔ Infrastructure の契約<br/>
/// データベース永続化のためのデータ構造を定義します。
/// </para>
/// 
/// <para><strong>Application/DTOs との違い</strong></para>
/// <list type="bullet">
/// <item>DTOs: Web.Api ⇔ Application（プレゼンテーション層との契約）</item>
/// <item>Contracts/Persistence: Application ⇔ Infrastructure（永続化層との契約）</item>
/// </list>
/// 
/// <para><strong>命名規則</strong></para>
/// <para>
/// 「Record」サフィックス = データベーステーブルの構造<br/>
/// 例：OrderRecord, InventoryRecord
/// </para>
/// </remarks>
public record OrderRecord
{
    /// <summary>
    /// 注文ID（主キー、AUTO_INCREMENT）
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// 顧客ID
    /// </summary>
    public int CustomerId { get; init; }

    /// <summary>
    /// 注文日時（UTC）
    /// </summary>
    public DateTime CreatedAt { get; init; }
}
