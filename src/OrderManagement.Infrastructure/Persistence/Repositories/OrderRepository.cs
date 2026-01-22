using Dapper;
using OrderManagement.Application.Common;
using OrderManagement.Application.Repositories;
using OrderManagement.Domain.Entities;
using System.Data;

namespace OrderManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// 注文リポジトリの実装
/// </summary>
/// <remarks>
/// <para><strong>集約の永続化戦略</strong></para>
/// <list type="number">
/// <item>Order（親）を INSERT</item>
/// <item>生成された OrderId を取得</item>
/// <item>各 OrderDetail（子）に OrderId を設定して INSERT</item>
/// </list>
/// 
/// <para><strong>トランザクション保証</strong></para>
/// <para>
/// UnitOfWork から注入された Transaction により、
/// Order と OrderDetail の整合性が保証される。
/// </para>
/// </remarks>
public class OrderRepository(IDbSession session)
    : IOrderRepository
{
    /// <inheritdoc />
    public async Task<int> CreateAsync(Order order)
    {
        // 1. 注文（親）を INSERT
        const string orderSql = """
            INSERT INTO Orders (CustomerId, CreatedAt)
            VALUES (@CustomerId, @CreatedAt);
            SELECT last_insert_rowid();
            """;

        var orderId = await session.Connection.ExecuteScalarAsync<int>(orderSql, order, session.Transaction);

        // 2. 注文明細（子）を一括 INSERT
        if (order.Details.Count != 0)
        {
            const string detailSql = """
                INSERT INTO OrderDetails (OrderId, ProductId, Quantity, UnitPrice)
                VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice)
                """;

            // 各明細に OrderId を設定
            foreach (var detail in order.Details)
            {
                detail.OrderId = orderId;
            }

            await session.Connection.ExecuteAsync(detailSql, order.Details, session.Transaction);
        }

        return orderId;
    }

    /// <inheritdoc />
    public async Task<Order?> GetByIdAsync(int id)
    {
        // 注文を取得
        const string orderSql = "SELECT * FROM Orders WHERE Id = @Id";
        var order = await session.Connection.QueryFirstOrDefaultAsync<Order>(
            orderSql, new { Id = id }, session.Transaction);

        if (order == null)
            return null;

        // 注文明細を取得
        const string detailSql = "SELECT * FROM OrderDetails WHERE OrderId = @OrderId";
        var details = await session.Connection.QueryAsync<OrderDetail>(
            detailSql, new { OrderId = id }, session.Transaction);

        order.Details.AddRange(details);

        return order;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Order>> GetAllAsync()
    {
        // すべての注文を取得
        const string orderSql = "SELECT * FROM Orders ORDER BY CreatedAt DESC";
        var orders = (await session.Connection.QueryAsync<Order>(orderSql, session.Transaction))
            .ToList();

        if (orders.Count == 0)
            return orders;

        // すべての注文明細を一括取得
        var orderIds = orders.Select(o => o.Id).ToArray();
        const string detailSql = "SELECT * FROM OrderDetails WHERE OrderId IN @OrderIds";
        var allDetails = (await session.Connection.QueryAsync<OrderDetail>(
            detailSql, new { OrderIds = orderIds }, session.Transaction))
            .ToList();

        // 注文に明細を紐付け
        foreach (var order in orders)
        {
            var details = allDetails.Where(d => d.OrderId == order.Id);
            order.Details.AddRange(details);
        }

        return orders;
    }
}
