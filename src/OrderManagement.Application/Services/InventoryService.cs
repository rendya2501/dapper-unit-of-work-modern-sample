using OrderManagement.Application.Common;
using OrderManagement.Application.Contracts;
using OrderManagement.Application.Repositories;
using OrderManagement.Application.Services.Abstractions;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Exceptions;

namespace OrderManagement.Application.Services;

/// <summary>
/// 在庫サービスの実装
/// </summary>
/// <remarks>
/// 在庫管理のビジネスロジックを実装します。
/// </remarks>
public class InventoryService(
    IUnitOfWork uow,
    IInventoryRepository inventory,
    IAuditLogRepository auditLog) : IInventoryService
{
    /// <inheritdoc />
    public async Task<OperationResult<IEnumerable<Inventory>>> GetAllAsync()
    {
        var inventories = await inventory.GetAllAsync();
        return Result.Success(inventories);
    }

    /// <inheritdoc />
    public async Task<OperationResult<Inventory>> GetByProductIdAsync(int productId)
    {
        var inventoryEntity = await inventory.GetByProductIdAsync(productId);
        if (inventoryEntity is null)
        {
            return Result.NotFound($"Inventory not found for productId: {productId}");
        }
        return Result.Success(inventoryEntity);
    }

    /// <inheritdoc />
    public async Task<OperationResult<int>> CreateAsync(string productName, int stock, decimal unitPrice)
    {
        try
        {
            await uow.BeginTransactionAsync();

            var productId = await inventory.CreateAsync(new Inventory
            {
                ProductName = productName,
                Stock = stock,
                UnitPrice = unitPrice
            });

            await auditLog.CreateAsync(new AuditLog
            {
                Action = "INVENTORY_CREATED",
                Details = $"ProductId={productId}, Name={productName}, Stock={stock}, Price={unitPrice}",
                CreatedAt = DateTime.UtcNow
            });

            await uow.CommitAsync();
            return Result.Success(productId);
        }
        catch
        {
            await uow.RollbackAsync();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<OperationResult> UpdateAsync(int productId, string productName, int stock, decimal unitPrice)
    {
        try
        {
            await uow.BeginTransactionAsync();

            var existing = await inventory.GetByProductIdAsync(productId);
            if (existing is null)
            {
                await uow.RollbackAsync();
                return Result.NotFound($"Product not found for productId: {productId}");
            }

            await inventory.UpdateAsync(productId, productName, stock, unitPrice);

            await auditLog.CreateAsync(new AuditLog
            {
                Action = "INVENTORY_UPDATED",
                Details = $"ProductId={productId}, Name={productName}, Stock={stock}, Price={unitPrice}",
                CreatedAt = DateTime.UtcNow
            });

            await uow.CommitAsync();
            return Result.Success();
        }
        catch
        {
            await uow.RollbackAsync();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<OperationResult> DeleteAsync(int productId)
    {
        try
        {
            await uow.BeginTransactionAsync();

            var existing = await inventory.GetByProductIdAsync(productId);
            if (existing is null)
            {
                await uow.RollbackAsync();
                return Result.NotFound($"Product not found for productId: {productId}");
            }

            await inventory.DeleteAsync(productId);

            await auditLog.CreateAsync(new AuditLog
            {
                Action = "INVENTORY_DELETED",
                Details = $"ProductId={productId}, Name={existing.ProductName}",
                CreatedAt = DateTime.UtcNow
            });

            await uow.CommitAsync();
            return Result.Success();
        }
        catch
        {
            await uow.RollbackAsync();
            throw;
        }
    }
}
