using Application.Common;
using Application.Repositories;
using Domain.Common.Results;
using Domain.Inventory;
using Shared.Models;

namespace Application.Services;

/// <summary>
/// 在庫サービス
/// </summary>
/// <remarks>
/// 在庫管理のビジネスロジック
/// </remarks>
public class InventoryService(
    IUnitOfWork uow,
    IInventoryRepository inventoryRepo,
    IAuditLogRepository auditLogRepo)
{
    /// <summary>
    /// すべての在庫を取得します
    /// </summary>
    public async Task<Result<IEnumerable<Inventory>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var inventories = await inventoryRepo.GetAllAsync(cancellationToken);

        return Result.Success(inventories);
    }

    /// <summary>
    /// 商品IDを指定して在庫を取得します
    /// </summary>
    public async Task<Result<Inventory>> GetByProductIdAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var inventory = await inventoryRepo.GetByProductIdAsync(productId, cancellationToken);

        if (inventory is null)
        {
            return Result.Failure<Inventory>(InventoryErrors.NotFoundByProductId(productId));
        }

        return Result.Success(inventory);
    }

    /// <summary>
    /// 在庫を作成します
    /// </summary>
    /// <param name="productName">商品名</param>
    /// <param name="stock">在庫数</param>
    /// <param name="unitPrice">単価</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>作成された商品ID</returns>
    public async Task<Result<int>> CreateAsync(
        string productName,
        int stock,
        decimal unitPrice,
        CancellationToken cancellationToken = default)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            // ドメインモデルのファクトリメソッドで作成（Resultパターン）
            var createResult = Inventory.Create(productName, stock, unitPrice);
            if (createResult.IsFailure)
            {
                return Result.Failure<int>(createResult.Error!);
            }

            var inventory = createResult.Value;

            var productId = await inventoryRepo.CreateAsync(inventory, cancellationToken);

            // 監査ログ記録
            await auditLogRepo.CreateAsync(new AuditLogRecord
            {
                Action = "INVENTORY_CREATED",
                Details = $"ProductId={productId}, Name={productName}, Stock={stock}, Price={unitPrice}",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            return Result.Success(productId);

        }, cancellationToken);
    }

    /// <summary>
    /// 在庫を更新します
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="productName">商品名</param>
    /// <param name="stock">在庫数</param>
    /// <param name="unitPrice">単価</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    public async Task<Result> UpdateAsync(
        int productId,
        string productName,
        int stock,
        decimal unitPrice,
        CancellationToken cancellationToken = default)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            var inventory = await inventoryRepo.GetByProductIdAsync(productId, cancellationToken);
            if (inventory is null)
            {
                return Result.Failure(InventoryErrors.NotFoundByProductId(productId));
            }

            // ドメインモデルのビジネスロジックで更新（Resultパターン）
            var updateResult = inventory.Update(productName, stock, unitPrice);
            if (updateResult.IsFailure)
            {
                return updateResult;
            }

            // Repository で永続化
            await inventoryRepo.UpdateAsync(inventory, cancellationToken);

            // 監査ログ記録
            await auditLogRepo.CreateAsync(new AuditLogRecord
            {
                Action = "INVENTORY_UPDATED",
                Details = $"ProductId={productId}, Name={productName}, Stock={stock}, Price={unitPrice}",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            return Result.Success();

        }, cancellationToken);
    }

    /// <summary>
    /// 在庫を削除します
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    public async Task<Result> DeleteAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            var inventory = await inventoryRepo.GetByProductIdAsync(productId, cancellationToken);
            if (inventory is null)
            {
                return Result.Failure(InventoryErrors.NotFoundByProductId(productId));
            }

            await inventoryRepo.DeleteAsync(productId, cancellationToken);

            // 監査ログ記録
            await auditLogRepo.CreateAsync(new AuditLogRecord
            {
                Action = "INVENTORY_DELETED",
                Details = $"ProductId={productId}, Name={inventory.ProductName}",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            return Result.Success();

        }, cancellationToken);
    }
}
