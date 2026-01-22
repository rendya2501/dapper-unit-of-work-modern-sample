# Dapper Unit of Work Sample

**レガシー環境でDapperを使った安全なトランザクション管理の実装サンプル**

[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13-blue)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Dapper](https://img.shields.io/badge/Dapper-2.1-orange)](https://github.com/DapperLib/Dapper)
[![SQLite](https://img.shields.io/badge/SQLite-3-green)](https://www.sqlite.org/)

---

## 🎯 このプロジェクトについて

Dapperを使用した**レガシーコードベースに導入可能な**Unit of Workパターンの実装サンプルです。

### なぜこのサンプルを作ったのか

Dapperには組み込みのトランザクション管理がないため、複数のRepositoryを跨ぐ処理で以下の問題が発生します：

- ❌ トランザクションの渡し忘れ
- ❌ 異なるトランザクションを使ってしまう
- ❌ Commit/Rollback漏れ

このプロジェクトでは、**Unit of Workパターン**を使ってこれらの問題を構造的に防止します。

---

## 📦 採用パターン：Basic Unit of Work

このプロジェクトでは**Basic UoW パターン**を採用しています。

```csharp
// トランザクション管理を明示的に記述
await using var uow = _unitOfWorkFactory();
uow.BeginTransaction();

try
{
    await uow.Orders.CreateAsync(order);
    await uow.Inventory.UpdateStockAsync(productId, newStock);
    
    await uow.CommitAsync();
}
catch
{
    await uow.RollbackAsync();
    throw;
}
```

### なぜBasic UoWパターンを選んだのか

1. **レガシーコードへの導入が容易**
   - try-catch構造は既存コードと親和性が高い
   - トランザクション制御が明示的で理解しやすい

2. **愚直で分かりやすい**
   - Commit/Rollbackが明示されているため、動作が追いやすい
   - チームメンバー全員が理解できる

3. **ActionScopeパターンの課題**
   - 途中のreturnでもCommitされる（意図しない動作）
   - ラムダ式のスコープ管理が複雑
   - デバッグが難しい

詳細は [DESIGN.md](./DESIGN.md) を参照してください。

---

## 🚀 クイックスタート

### 前提条件

- .NET 10.0 SDK以上
- 任意のIDE（Visual Studio / Rider / VS Code）

### 1. リポジトリをクローン

```bash
git clone https://github.com/rendya2501/Dapper.UnitOfWork.Sample.git
cd Dapper.UnitOfWork.Sample
```

### 2. プロジェクトを実行

```bash
cd OrderManagement.Api
dotnet run
```

### 3. APIを試す

ブラウザで http://localhost:5076/scalar/v1 を開く

**または**

```bash
# 注文を作成
curl -X POST http://localhost:5076/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": 1,
    "items": [
      { "productId": 1, "quantity": 2 }
    ]
  }'
```

---

## 📖 基本的な使い方

### DI登録

```csharp
// Program.cs
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found.");

// IDbConnectionを登録
builder.Services.AddScoped<IDbConnection>(_ => 
    new SqliteConnection(connectionString));

// UnitOfWorkをFactory形式で登録
builder.Services.AddScoped<Func<IUnitOfWork>>(sp =>
{
    var connection = sp.GetRequiredService<IDbConnection>();
    return () => new UnitOfWork(connection);
});

// Serviceを登録
builder.Services.AddScoped<IOrderService, OrderService>();
```

### Service層での使用

#### 基本形：単一Repository操作

```csharp
public class OrderService
{
    private readonly Func<IUnitOfWork> _unitOfWorkFactory;

    public OrderService(Func<IUnitOfWork> unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<Order?> GetOrderAsync(int orderId)
    {
        // 読み取り専用はトランザクション不要
        await using var uow = _unitOfWorkFactory();
        return await uow.Orders.GetByIdAsync(orderId);
    }
}
```

#### 応用形：複数Repository横断操作

```csharp
public async Task<int> CreateOrderAsync(CreateOrderRequest request)
{
    await using var uow = _unitOfWorkFactory();
    uow.BeginTransaction();

    try
    {
        // 1. 在庫確認
        var inventory = await uow.Inventory.GetByProductIdAsync(request.ProductId);
        if (inventory == null)
            throw new NotFoundException($"Product {request.ProductId} not found");
        
        if (inventory.Stock < request.Quantity)
            throw new InsufficientStockException();

        // 2. 在庫減算
        await uow.Inventory.UpdateStockAsync(
            request.ProductId, 
            inventory.Stock - request.Quantity
        );

        // 3. 注文作成
        var order = new Order
        {
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            CreatedAt = DateTime.UtcNow
        };
        var orderId = await uow.Orders.CreateAsync(order);

        // 4. 監査ログ記録
        await uow.AuditLogs.CreateAsync(new AuditLog
        {
            Action = "ORDER_CREATED",
            Details = $"OrderId={orderId}, ProductId={request.ProductId}",
            CreatedAt = DateTime.UtcNow
        });

        // すべて成功 → Commit
        await uow.CommitAsync();
        return orderId;
    }
    catch
    {
        // 例外発生 → Rollback
        await uow.RollbackAsync();
        throw;
    }
}
```

---

## 🏗️ プロジェクト構成

```
Dapper.UnitOfWork.Sample/
│
├── OrderManagement.Api/              # Web API層
│   ├── Controllers/                  # エンドポイント
│   ├── Middleware/                   # エラーハンドリング
│   └── Program.cs                    # DI設定・起動
│
├── OrderManagement.Application/      # ビジネスロジック層
│   └── Services/
│       ├── OrderService.cs          # 複数Repository横断処理
│       └── InventoryService.cs
│
├── OrderManagement.Domain/           # ドメイン層
│   ├── Entities/
│   │   ├── Order.cs
│   │   ├── Inventory.cs
│   │   └── AuditLog.cs
│   └── Exceptions/
│
└── OrderManagement.Infrastructure/   # データアクセス層
    ├── UnitOfWork/
    │   ├── IUnitOfWork.cs           # 基本インターフェース
    │   └── UnitOfWork.cs            # トランザクション管理
    │
    ├── Repositories/
    │   ├── Abstractions/            # Repository インターフェース
    │   ├── OrderRepository.cs
    │   ├── InventoryRepository.cs
    │   └── AuditLogRepository.cs
    │
    └── Database/
        └── DatabaseInitializer.cs   # スキーマ初期化
```

---

## 💡 よくあるパターン

### 1. 複数分岐があるトランザクション処理

```csharp
public async Task<ProcessResult> ProcessOrderAsync(OrderRequest request)
{
    await using var uow = _unitOfWorkFactory();
    uow.BeginTransaction();

    try
    {
        var inventory = await uow.Inventory.GetByProductIdAsync(request.ProductId);
        
        // 在庫不足の場合は早期return
        if (inventory.Stock < request.Quantity)
        {
            await uow.RollbackAsync();  // 明示的にRollback
            return new ProcessResult { Success = false, Message = "在庫不足" };
        }

        // 通常処理
        await uow.Inventory.UpdateStockAsync(request.ProductId, inventory.Stock - request.Quantity);
        var orderId = await uow.Orders.CreateAsync(order);

        await uow.CommitAsync();
        return new ProcessResult { Success = true, OrderId = orderId };
    }
    catch
    {
        await uow.RollbackAsync();
        throw;
    }
}
```

### 2. 長時間処理との組み合わせ

```csharp
public async Task ProcessOrderWithNotificationAsync(OrderRequest request)
{
    int orderId;

    // トランザクション内では最小限の処理のみ
    await using (var uow = _unitOfWorkFactory())
    {
        uow.BeginTransaction();
        try
        {
            orderId = await uow.Orders.CreateAsync(order);
            await uow.CommitAsync();
        }
        catch
        {
            await uow.RollbackAsync();
            throw;
        }
    }

    // トランザクション外で長時間処理
    await _emailService.SendOrderConfirmationAsync(orderId);
    await _smsService.SendNotificationAsync(orderId);
}
```

### 3. バッチ処理

```csharp
public async Task ProcessBatchOrdersAsync(List<OrderRequest> requests)
{
    await using var uow = _unitOfWorkFactory();
    uow.BeginTransaction();

    try
    {
        foreach (var request in requests)
        {
            var inventory = await uow.Inventory.GetByProductIdAsync(request.ProductId);
            
            if (inventory.Stock >= request.Quantity)
            {
                await uow.Inventory.UpdateStockAsync(
                    request.ProductId,
                    inventory.Stock - request.Quantity
                );
                await uow.Orders.CreateAsync(new Order { /* ... */ });
            }
        }

        // バッチ全体を1トランザクションでCommit
        await uow.CommitAsync();
    }
    catch
    {
        await uow.RollbackAsync();
        throw;
    }
}
```

---

## ✅ ベストプラクティス

### 1. トランザクションは最小限に保つ

```csharp
// ✅ 良い例：DB操作のみトランザクション内
var orderId = await CreateOrderInTransaction(request);
await SendNotificationOutsideTransaction(orderId);

// ❌ 悪い例：外部API呼び出しまでトランザクション内
await using var uow = _unitOfWorkFactory();
uow.BeginTransaction();
try
{
    var orderId = await uow.Orders.CreateAsync(order);
    await _externalApi.CallAsync();  // トランザクションが長時間ロック
    await uow.CommitAsync();
}
catch { await uow.RollbackAsync(); throw; }
```

### 2. 早期returnではRollbackを明示

```csharp
await using var uow = _unitOfWorkFactory();
uow.BeginTransaction();

try
{
    if (invalidCondition)
    {
        await uow.RollbackAsync();  // 必ず明示
        return;
    }
    
    // 処理続行
    await uow.CommitAsync();
}
catch
{
    await uow.RollbackAsync();
    throw;
}
```

### 3. Repositoryは純粋にデータアクセスのみ

```csharp
// Repository: トランザクション管理は一切しない
public class OrderRepository : IOrderRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    public async Task<int> CreateAsync(Order order)
    {
        return await _connection.ExecuteScalarAsync<int>(
            sql, order, _transaction);  // Transactionを渡すだけ
    }
}

// Service: トランザクション管理とビジネスロジック
public class OrderService
{
    public async Task<int> CreateOrderAsync(...)
    {
        uow.BeginTransaction();
        try
        {
            // ビジネスロジック + Repository呼び出し
            await uow.CommitAsync();
        }
        catch { await uow.RollbackAsync(); throw; }
    }
}
```

---

## 🔍 トラブルシューティング

### トランザクションがコミットされない

**原因**: 例外が発生してRollbackされている

**解決策**: ログを確認し、例外の原因を特定

```json
// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "OrderManagement": "Debug"
    }
  }
}
```

### デッドロックが発生する

**原因**: トランザクション内で長時間処理を実行している

**解決策**: 外部APIコールや重い処理はトランザクション外へ

### Repositoryでトランザクションが効かない

**原因**: UnitOfWorkから取得したRepositoryを使っていない

**解決策**: 必ずUnitOfWork経由でRepositoryを取得

```csharp
// ❌ 悪い例
var repo = new OrderRepository(connection, null);

// ✅ 良い例
await using var uow = _unitOfWorkFactory();
var order = await uow.Orders.GetByIdAsync(orderId);
```

---

## 📚 詳細情報

### 設計ドキュメント

- [DESIGN.md](./DESIGN.md) - 設計思想とパターン比較

### 参考資料

- [Martin Fowler - Unit of Work](https://martinfowler.com/eaaCatalog/unitOfWork.html)
- [Microsoft - Repository Pattern](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)
- [Dapper Documentation](https://github.com/DapperLib/Dapper)

---

## 🧪 テストの実行

```bash
cd Tests
dotnet test
```
