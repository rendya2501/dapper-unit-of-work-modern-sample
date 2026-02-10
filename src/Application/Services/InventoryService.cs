using Application.Common;
using Application.Repositories;
using Domain.AuditLog;
using Domain.Common.Results;
using Domain.Inventory;

namespace Application.Services;

/// <summary>
/// 在庫サービスの実装
/// </summary>
/// <remarks>
/// 在庫管理のビジネスロジックを実装します。
/// </remarks>
public class InventoryService(
    IUnitOfWork uow,
    IInventoryRepository inventory,
    IAuditLogRepository auditLog)
{
    /// <inheritdoc />
    public async Task<Result<IEnumerable<Inventory>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var inventories = await inventory.GetAllAsync(cancellationToken);
        return Result.Success(inventories);
    }

    /// <inheritdoc />
    public async Task<Result<Inventory>> GetByProductIdAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var inventoryEntity = await inventory.GetByProductIdAsync(productId, cancellationToken);
        if (inventoryEntity is null)
        {
            return Result.Failure<Inventory>(InventoryErrors.NotFoundByProductId(productId));
        }
        return Result.Success(inventoryEntity);
    }

    /// <inheritdoc />
    public async Task<Result<int>> CreateAsync(
        string productName,
        int stock,
        decimal unitPrice,
        CancellationToken cancellationToken = default)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            var productId = await inventory.CreateAsync(new Inventory
            {
                ProductName = productName,
                Stock = stock,
                UnitPrice = unitPrice
            }, cancellationToken);

            await auditLog.CreateAsync(new AuditLog
            {
                Action = "INVENTORY_CREATED",
                Details = $"ProductId={productId}, Name={productName}, Stock={stock}, Price={unitPrice}",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            return Result.Success(productId);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAsync(
        int productId,
        string productName,
        int stock,
        decimal unitPrice,
        CancellationToken cancellationToken = default)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            var existing = await inventory.GetByProductIdAsync(productId, cancellationToken);
            if (existing is null)
            {
                return Result.Failure(InventoryErrors.NotFoundByProductId(productId));
            }

            await inventory.UpdateAsync(productId, productName, stock, unitPrice, cancellationToken);

            await auditLog.CreateAsync(new AuditLog
            {
                Action = "INVENTORY_UPDATED",
                Details = $"ProductId={productId}, Name={productName}, Stock={stock}, Price={unitPrice}",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            return Result.Success();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            var existing = await inventory.GetByProductIdAsync(productId, cancellationToken);
            if (existing is null)
            {
                return Result.Failure(InventoryErrors.NotFoundByProductId(productId));
            }

            await inventory.DeleteAsync(productId, cancellationToken);

            await auditLog.CreateAsync(new AuditLog
            {
                Action = "INVENTORY_DELETED",
                Details = $"ProductId={productId}, Name={existing.ProductName}",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            return Result.Success();
        }, cancellationToken);
    }
}
