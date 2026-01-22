using Dapper;
using OrderManagement.Application.Common;
using OrderManagement.Application.Repositories;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// 在庫リポジトリの実装
/// </summary>
/// <remarks>
/// <para><strong>設計原則</strong></para>
/// <list type="bullet">
/// <item>Repository は session.Connection と session.Transaction を受け取るが、Begin/Commit/Rollback は一切行わない</item>
/// <item>トランザクション管理は UnitOfWork が責任を持つ</item>
/// <item>Repository は純粋にデータアクセスのみに専念</item>
/// </list>
/// </remarks>
/// <param name="session.Connection">データベース接続</param>
/// <param name="session.Transaction">トランザクション（UnitOfWork から注入）</param>
public class InventoryRepository(IDbSession session)
    : IInventoryRepository
{
    /// <inheritdoc />
    public async Task<Inventory?> GetByProductIdAsync(int productId)
    {
        const string sql = "SELECT * FROM Inventory WHERE ProductId = @ProductId";

        return await session.Connection.QueryFirstOrDefaultAsync<Inventory>(
            sql, new { ProductId = productId }, session.Transaction);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Inventory>> GetAllAsync()
    {
        const string sql = "SELECT * FROM Inventory ORDER BY ProductId";

        return await session.Connection.QueryAsync<Inventory>(sql, session.Transaction);
    }

    /// <inheritdoc />
    public async Task<int> CreateAsync(Inventory inventory)
    {
        const string sql = """
            INSERT INTO Inventory (ProductName, Stock, UnitPrice)
            VALUES (@ProductName, @Stock, @UnitPrice);
            SELECT last_insert_rowid();
            """;
        return await session.Connection.ExecuteScalarAsync<int>(sql, inventory, session.Transaction);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(int productId, string productName, int stock, decimal unitPrice)
    {
        const string sql = """
            UPDATE Inventory 
            SET ProductName = @ProductName, Stock = @Stock, UnitPrice = @UnitPrice
            WHERE ProductId = @ProductId
            """;

        await session.Connection.ExecuteAsync(sql,
            new { ProductId = productId, ProductName = productName, Stock = stock, UnitPrice = unitPrice },
            session.Transaction);
    }

    /// <inheritdoc />
    public async Task<int> UpdateStockAsync(int productId, int newStock)
    {
        const string sql = "UPDATE Inventory SET Stock = @Stock WHERE ProductId = @ProductId";

        return await session.Connection.ExecuteAsync(
            sql, new { ProductId = productId, Stock = newStock }, session.Transaction);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int productId)
    {
        const string sql = "DELETE FROM Inventory WHERE ProductId = @ProductId";

        await session.Connection.ExecuteAsync(sql, new { ProductId = productId }, session.Transaction);
    }
}