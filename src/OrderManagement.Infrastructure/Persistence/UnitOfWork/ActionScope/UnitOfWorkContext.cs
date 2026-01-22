using OrderManagement.Application.Common;
using OrderManagement.Application.Repositories;
using OrderManagement.Infrastructure.Persistence.Repositories;

namespace OrderManagement.Infrastructure.Persistence.UnitOfWork.ActionScope;

/// <summary>
/// Unit of Work コンテキストの実装
/// </summary>
internal class UnitOfWorkContext(IDbSession accessor) : IUnitOfWorkContext
{
    /// <inheritdoc />
    public IOrderRepository Orders
        => field ??= new OrderRepository(accessor);

    /// <inheritdoc />
    public IInventoryRepository Inventory
        => field ??= new InventoryRepository(accessor);

    /// <inheritdoc />
    public IAuditLogRepository AuditLogs
        => field ??= new AuditLogRepository(accessor);
}
