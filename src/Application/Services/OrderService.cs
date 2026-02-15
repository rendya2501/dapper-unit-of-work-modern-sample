using Application.Common;
using Application.DTOs;
using Application.Repositories;
using Domain.Inventory;
using Domain.Orders;
using SharedKernel.Models;
using SharedKernel.Primitives;

namespace Application.Services;

/// <summary>
/// 注文サービス
/// </summary>
/// <remarks>
/// <para><strong>ビジネスロジック</strong></para>
/// <list type="bullet">
/// <item>在庫確認・減算</item>
/// <item>注文集約の構築</item>
/// <item>トランザクション境界の管理</item>
/// <item>監査ログの記録</item>
/// </list>
/// </remarks>
/// <param name="uow">Unit of Work（DI経由で注入）</param>
public class OrderService(
    IUnitOfWork uow,
    IInventoryRepository inventoryRepo,
    IOrderRepository orderRepo,
    IAuditLogRepository auditLogRepo)
{
    /// <summary>
    /// 注文を作成します
    /// </summary>
    /// <param name="customerId">顧客ID</param>
    /// <param name="items">注文する商品と数量のリスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>作成された注文ID</returns>
    public async Task<Result<int>> CreateOrderAsync(
        int customerId,
        List<OrderItem> items,
        CancellationToken cancellationToken)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            // 1. 注文アイテムチェック
            if (items.Count == 0)
            {
                return Result.Failure<int>(OrderErrors.EmptyOrder());
            }

            // 2. 注文集約を構築（Resultパターン）
            var orderResult = Order.Create(customerId);
            if (orderResult.IsFailure)
            {
                return Result.Failure<int>(orderResult.Error!);
            }

            var order = orderResult.Value;

            // 3. 各商品の在庫確認と注文明細追加
            foreach (var item in items)
            {
                // Repository → Record取得
                var inventory = await inventoryRepo.GetByProductIdAsync(item.ProductId, cancellationToken);
                if (inventory is null)
                {
                    return Result.Failure<int>(InventoryErrors.NotFoundByProductId(item.ProductId));
                }

                // 4. 在庫減算（Resultパターン - try-catch不要）
                var decreaseResult = inventory.Decrease(item.Quantity);
                if (decreaseResult.IsFailure)
                {
                    return Result.Failure<int>(decreaseResult.Error!);
                }

                // 5. 在庫更新
                await inventoryRepo.UpdateStockAsync(
                    item.ProductId,
                    inventory.Stock,
                    cancellationToken);

                // 6. 注文明細追加（Resultパターン）
                var addDetailResult = order.AddDetail(
                    new ProductId(item.ProductId),
                    item.Quantity,
                    inventory.UnitPrice);

                if (addDetailResult.IsFailure)
                {
                    return Result.Failure<int>(addDetailResult.Error!);
                }
            }

            // 7. Repository呼び出し（変換はRepository内部）
            var orderId = await orderRepo.CreateAsync(order, cancellationToken);

            // 8. 監査ログ記録（Shared Kernel使用）
            await auditLogRepo.CreateAsync(new AuditLogRecord
            {
                Action = "ORDER_CREATED",
                Details = $"OrderId={orderId}, CustomerId={customerId}, " +
                    $"Items={items.Count}, Total={order.TotalAmount:C}",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            return Result.Success(orderId);

        }, cancellationToken);
    }

    /// <summary>
    /// すべての注文を取得します
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>注文のリスト</returns>
    public async Task<Result<IEnumerable<Order>>> GetAllOrdersAsync(
        CancellationToken cancellationToken = default)
    {
        var orders = await orderRepo.GetAllAsync(cancellationToken);

        return Result.Success(orders);
    }

    /// <summary>
    /// IDを指定して注文を取得します
    /// </summary>
    /// <param name="id">注文ID</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>注文</returns>
    public async Task<Result<Order>> GetOrderByIdAsync(int id,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepo.GetByIdAsync(id, cancellationToken);

        if (order is null)
        {
            return Result.Failure<Order>(OrderErrors.NotFoundByOrderId(id));
        }

        return Result.Success(order);
    }
}
