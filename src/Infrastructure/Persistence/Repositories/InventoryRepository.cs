using Application.Common;
using Application.Repositories;
using Dapper;
using Domain.Inventory;
using Infrastructure.Persistence.Mappers;
using Infrastructure.Persistence.Models;

namespace Infrastructure.Persistence.Repositories;

/// <summary>
/// 在庫リポジトリの実装（Domain/Persistence分離版）
/// </summary>
/// <remarks>
/// Mapperを使用してドメインモデルと永続化モデルを完全分離。
/// Dapperは InventoryRecord にマッピングし、返却時にドメインモデルに変換。
/// </remarks>
public class InventoryRepository(IDbSession session)
    : IInventoryRepository
{
    /// <inheritdoc />
    public async Task<Inventory?> GetByProductIdAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM Inventory WHERE ProductId = @ProductId";

        var command = new CommandDefinition(
            sql,
            new { ProductId = productId },
            session.Transaction,
            cancellationToken: cancellationToken);

        var record = await session.Connection.QueryFirstOrDefaultAsync<InventoryRecord>(command);

        // Record → Domain 変換（Repository内部）
        return record == null ? null : InventoryMapper.ToDomain(record);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Inventory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM Inventory ORDER BY ProductId";

        var command = new CommandDefinition(
            sql,
            session.Transaction,
            cancellationToken: cancellationToken);

        var records = await session.Connection.QueryAsync<InventoryRecord>(command);

        // Record → Domain 変換（Repository内部）
        return records.Select(InventoryMapper.ToDomain);
    }

    /// <inheritdoc />
    public async Task<int> CreateAsync(
        Inventory inventory,
        CancellationToken cancellationToken = default)
    {
        // Domain → Record 変換（Repository内部）
        var record = InventoryMapper.ToRecord(inventory);

        const string sql = """
            INSERT INTO Inventory (ProductName, Stock, UnitPrice)
            VALUES (@ProductName, @Stock, @UnitPrice);
            SELECT last_insert_rowid();
            """;

        var command = new CommandDefinition(
            sql,
            record,
            session.Transaction,
            cancellationToken: cancellationToken);

        var productId = await session.Connection.ExecuteScalarAsync<int>(command);

        // Domain側にIDを設定
        inventory.SetId(new ProductId(productId));

        return productId;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(
        Inventory inventory,
        CancellationToken cancellationToken = default)
    {
        // Domain → Record 変換（Repository内部）
        var record = InventoryMapper.ToRecord(inventory);

        const string sql = """
            UPDATE Inventory 
            SET ProductName = @ProductName, Stock = @Stock, UnitPrice = @UnitPrice
            WHERE ProductId = @ProductId
            """;

        var command = new CommandDefinition(
            sql,
            record,
            session.Transaction,
            cancellationToken: cancellationToken);

        await session.Connection.ExecuteAsync(command);
    }

    /// <inheritdoc />
    public async Task<int> UpdateStockAsync(
        int productId,
        int newStock,
        CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE Inventory SET Stock = @Stock WHERE ProductId = @ProductId";

        var command = new CommandDefinition(
            sql,
            new { ProductId = productId, Stock = newStock },
            session.Transaction,
            cancellationToken: cancellationToken);

        return await session.Connection.ExecuteAsync(command);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int productId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM Inventory WHERE ProductId = @ProductId";

        var command = new CommandDefinition(
            sql,
            new { ProductId = productId },
            session.Transaction,
            cancellationToken: cancellationToken);

        await session.Connection.ExecuteAsync(command);
    }
}