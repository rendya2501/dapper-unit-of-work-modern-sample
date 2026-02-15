using Application.Common;
using Application.Repositories;
using Dapper;
using Domain.Orders;
using Infrastructure.Persistence.Mappers;
using Infrastructure.Persistence.Models;

namespace Infrastructure.Persistence.Repositories;

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
    public async Task<int> CreateAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        // 1. Domain → Record 変換（Repository内部）
        var (orderRecord, detailRecords) = OrderMapper.ToRecords(order);

        // 2. Order INSERT
        const string orderSql = """
            INSERT INTO Orders (CustomerId, CreatedAt)
            VALUES (@CustomerId, @CreatedAt);
            SELECT last_insert_rowid();
            """;

        var orderCommand = new CommandDefinition(
            orderSql,
            orderRecord,
            session.Transaction,
            cancellationToken: cancellationToken);

        var orderId = await session.Connection.ExecuteScalarAsync<int>(orderCommand);

        // 3. Domain側にIDを設定
        order.SetId(new OrderId(orderId));


        // 4. 注文明細（子）を一括 INSERT
        if (detailRecords.Any())
        {
            const string detailSql = """
                INSERT INTO OrderDetails (OrderId, ProductId, Quantity, UnitPrice)
                VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice)
                """;

            // 各明細に OrderId を設定
            foreach (var detail in detailRecords)
            {
                detail.OrderId = orderId;
            }

            var detailCommand = new CommandDefinition(
                detailSql,
                detailRecords,
                session.Transaction,
                cancellationToken: cancellationToken);

            await session.Connection.ExecuteAsync(detailCommand);
        }

        return orderId;
    }

    /// <inheritdoc />
    public async Task<Order?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT 
                o.*, 
                d.*
            FROM Orders o
            LEFT JOIN OrderDetails d ON o.Id = d.OrderId
            WHERE o.Id = @Id
            """;

        var recordDict = new Dictionary<int, (OrderRecord Order, List<OrderDetailRecord> Details)>();

        var command = new CommandDefinition(
            sql,
            parameters: new { Id = id },
            transaction: session.Transaction,
            cancellationToken: cancellationToken);

        // Dapperで永続化モデルにマッピング
        await session.Connection.QueryAsync<OrderRecord, OrderDetailRecord, OrderRecord>(
            command,
            (orderRecord, detailRecord) =>
            {
                if (!recordDict.TryGetValue(orderRecord.Id, out var existing))
                {
                    existing = (orderRecord, new List<OrderDetailRecord>());
                    recordDict.Add(orderRecord.Id, existing);
                }

                if (detailRecord != null)
                {
                    existing.Details.Add(detailRecord);
                }

                return orderRecord;
            },
            splitOn: "Id"
        );

        if (recordDict.Count == 0)
        {
            return null;
        }

        // Record → Domain 変換（Repository内部）
        var (orderRec, detailRecs) = recordDict.Values.First();
        return OrderMapper.ToDomain(orderRec, detailRecs);


        //// 注文を取得
        //const string orderSql = "SELECT * FROM Orders WHERE Id = @Id";
        //var order = await session.Connection.QueryFirstOrDefaultAsync<Order>(
        //    orderSql, new { Id = id }, session.Transaction);

        //if (order == null)
        //    return null;

        //// 注文明細を取得
        //const string detailSql = "SELECT * FROM OrderDetails WHERE OrderId = @OrderId";
        //var details = await session.Connection.QueryAsync<OrderDetail>(
        //    detailSql, new { OrderId = id }, session.Transaction);

        //order.Details.AddRange(details);

        //return order;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Order>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT 
                o.*, 
                d.*
            FROM Orders o
            LEFT JOIN OrderDetails d ON o.Id = d.OrderId
            ORDER BY o.CreatedAt DESC
            """;

        var recordDict = new Dictionary<int, (OrderRecord Order, List<OrderDetailRecord> Details)>();

        var command = new CommandDefinition(
            sql,
            transaction: session.Transaction,
            cancellationToken: cancellationToken);

        // Dapperで永続化モデルにマッピング
        await session.Connection.QueryAsync<OrderRecord, OrderDetailRecord, OrderRecord>(
            command,
            (orderRecord, detailRecord) =>
            {
                if (!recordDict.TryGetValue(orderRecord.Id, out var existing))
                {
                    existing = (orderRecord, new List<OrderDetailRecord>());
                    recordDict.Add(orderRecord.Id, existing);
                }

                if (detailRecord != null)
                {
                    existing.Details.Add(detailRecord);
                }

                return orderRecord;
            },
            splitOn: "Id"
        );

        // Record → Domain 変換（Repository内部）
        return recordDict.Values
            .Select(r => OrderMapper.ToDomain(r.Order, r.Details));


        //// すべての注文を取得
        //const string orderSql = "SELECT * FROM Orders ORDER BY CreatedAt DESC";
        //var orders = (await session.Connection.QueryAsync<Order>(orderSql, session.Transaction))
        //    .ToList();

        //if (orders.Count == 0)
        //    return orders;

        //// すべての注文明細を一括取得
        //var orderIds = orders.Select(o => o.Id).ToArray();
        //const string detailSql = "SELECT * FROM OrderDetails WHERE OrderId IN @OrderIds";
        //var allDetails = (await session.Connection.QueryAsync<OrderDetail>(
        //    detailSql, new { OrderIds = orderIds }, session.Transaction))
        //    .ToList();

        //// 注文に明細を紐付け
        //foreach (var order in orders)
        //{
        //    var details = allDetails.Where(d => d.OrderId == order.Id);
        //    order.Details.AddRange(details);
        //}

        //return orders;
    }
}
