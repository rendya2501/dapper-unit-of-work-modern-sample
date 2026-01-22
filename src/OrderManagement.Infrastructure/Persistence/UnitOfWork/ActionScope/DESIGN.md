# Design Document - Unit of Work Pattern

このドキュメントでは、プロジェクトで採用したUnit of Workパターンの設計思想と、検討したパターンの比較について説明します。

---

## 📋 目次

- [設計目標](#設計目標)
- [Dapperのトランザクション管理における課題](#dapperのトランザクション管理における課題)
- [採用パターン：Basic Unit of Work](#採用パターンbasic-unit-of-work)
- [検討したパターン：ActionScope](#検討したパターンactionscope)
- [なぜBasic UoWを選んだのか](#なぜbasic-uowを選んだのか)
- [アーキテクチャ詳細](#アーキテクチャ詳細)
- [トレードオフ](#トレードオフ)

---

## 設計目標

このプロジェクトは、以下の目標を達成するために設計されました：

### 1. レガシーコードベースへの導入可能性

- 既存のコードベースに段階的に導入できる
- 大規模なリファクタリングを必要としない
- チーム全体が理解できるシンプルさ

### 2. トランザクション管理の安全性

- トランザクションの渡し忘れを構造的に防止
- 複数Repositoryを跨ぐ処理で確実にトランザクションを共有
- Commit/Rollback漏れを最小化

### 3. 保守性の高さ

- 動作が追いやすい
- デバッグが容易
- 新しいメンバーでもすぐに理解できる

---

## Dapperのトランザクション管理における課題

Dapperは軽量で高速なマッパーライブラリですが、**組み込みのトランザクション管理機構を持ちません**。

### 典型的な問題コード

```csharp
// ❌ 問題1: トランザクション渡し忘れ
public async Task CreateOrderAsync(Order order)
{
    using var transaction = _connection.BeginTransaction();
    
    await _orderRepository.CreateAsync(order);  // transaction渡し忘れ！
    await _inventoryRepository.UpdateAsync(inventory, transaction);
    
    transaction.Commit();
}

// ❌ 問題2: 異なるトランザクションを使用
public async Task CreateOrderAsync(Order order)
{
    using var tx1 = _connection.BeginTransaction();
    using var tx2 = _connection.BeginTransaction();  // 別のトランザクション！
    
    await _orderRepository.CreateAsync(order, tx1);
    await _inventoryRepository.UpdateAsync(inventory, tx2);  // アトミック性が保証されない
    
    tx1.Commit();
    tx2.Commit();
}

// ❌ 問題3: Commit/Rollback漏れ
public async Task CreateOrderAsync(Order order)
{
    using var transaction = _connection.BeginTransaction();
    
    await _orderRepository.CreateAsync(order, transaction);
    await _inventoryRepository.UpdateAsync(inventory, transaction);
    
    // Commitを書き忘れている！
}
```

### Unit of Workによる解決

これらの問題を**構造的に防止**するために、Unit of Workパターンを導入します。

---

## 採用パターン：Basic Unit of Work

### 概要

**手動でトランザクションを制御する明示的なパターン**

```csharp
await using var uow = _unitOfWorkFactory();
uow.BeginTransaction();

try
{
    await uow.Orders.CreateAsync(order);
    await uow.Inventory.UpdateStockAsync(productId, newStock);
    
    await uow.CommitAsync();  // 明示的なCommit
}
catch
{
    await uow.RollbackAsync();  // 明示的なRollback
    throw;
}
```

### アーキテクチャ

```
┌─────────────────────────────────────────┐
│         IUnitOfWork                     │
│  ┌─────────────────────────────────┐   │
│  │ BeginTransaction()               │   │
│  │ CommitAsync()                    │   │
│  │ RollbackAsync()                  │   │
│  └─────────────────────────────────┘   │
│  ┌─────────────────────────────────┐   │
│  │ Orders: IOrderRepository         │   │
│  │ Inventory: IInventoryRepository  │   │
│  │ AuditLogs: IAuditLogRepository   │   │
│  └─────────────────────────────────┘   │
└─────────────────────────────────────────┘
                    │
                    ▼ 生成時にConnection/Transactionを注入
┌─────────────────────────────────────────┐
│      Repository                         │
│  ┌─────────────────────────────────┐   │
│  │ _connection: IDbConnection       │   │
│  │ _transaction: IDbTransaction?    │   │
│  │                                  │   │
│  │ CreateAsync(entity)              │   │
│  │   → Dapper.ExecuteAsync(...,    │   │
│  │              _transaction)       │   │
│  └─────────────────────────────────┘   │
└─────────────────────────────────────────┘
```

### 実装の核心部分

#### IUnitOfWork インターフェース

```csharp
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    // トランザクション制御
    void BeginTransaction();
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
    
    // Repository取得（UoWが生成・管理）
    IOrderRepository Orders { get; }
    IInventoryRepository Inventory { get; }
    IAuditLogRepository AuditLogs { get; }
}
```

#### UnitOfWork 実装

```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly IDbConnection _connection;
    private IDbTransaction? _transaction;
    
    // Repository インスタンス（遅延初期化）
    private IOrderRepository? _orderRepository;
    private IInventoryRepository? _inventoryRepository;
    private IAuditLogRepository? _auditLogRepository;

    public UnitOfWork(IDbConnection connection)
    {
        _connection = connection;
        if (_connection.State != ConnectionState.Open)
            _connection.Open();
    }

    public void BeginTransaction()
    {
        if (_transaction != null)
            throw new InvalidOperationException("Transaction already started");
        
        _transaction = _connection.BeginTransaction();
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("Transaction not started");

        await Task.Run(() => _transaction.Commit(), cancellationToken);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("Transaction not started");

        await Task.Run(() => _transaction.Rollback(), cancellationToken);
    }

    // Repository取得（重要：ここで同一のConnection/Transactionを注入）
    public IOrderRepository Orders
        => _orderRepository ??= new OrderRepository(_connection, _transaction);

    public IInventoryRepository Inventory
        => _inventoryRepository ??= new InventoryRepository(_connection, _transaction);

    public IAuditLogRepository AuditLogs
        => _auditLogRepository ??= new AuditLogRepository(_connection, _transaction);
}
```

### 設計のポイント

#### 1. Repositoryの生成をUnitOfWorkが管理

```csharp
// RepositoryはUnitOfWork内部で生成される
public IOrderRepository Orders
    => _orderRepository ??= new OrderRepository(_connection, _transaction);
```

これにより：
- すべてのRepositoryが**同一のConnection**を使用
- すべてのRepositoryが**同一のTransaction**を使用
- トランザクション渡し忘れが**構造的に不可能**

#### 2. Repositoryはトランザクション管理を一切しない

```csharp
public class OrderRepository : IOrderRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    public OrderRepository(IDbConnection connection, IDbTransaction? transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<int> CreateAsync(Order order)
    {
        const string sql = @"
            INSERT INTO Orders (ProductId, Quantity, CreatedAt)
            VALUES (@ProductId, @Quantity, @CreatedAt);
            SELECT last_insert_rowid();";

        // Transactionを渡すだけ。Begin/Commit/Rollbackは一切しない
        return await _connection.ExecuteScalarAsync<int>(sql, order, _transaction);
    }
}
```

#### 3. Service層でトランザクション境界を明確化

```csharp
public async Task<int> CreateOrderAsync(CreateOrderRequest request)
{
    // ===== トランザクション境界開始 =====
    await using var uow = _unitOfWorkFactory();
    uow.BeginTransaction();

    try
    {
        // ビジネスロジック
        var inventory = await uow.Inventory.GetByProductIdAsync(request.ProductId);
        await uow.Inventory.UpdateStockAsync(productId, newStock);
        var orderId = await uow.Orders.CreateAsync(order);
        await uow.AuditLogs.CreateAsync(log);

        await uow.CommitAsync();
        return orderId;
    }
    catch
    {
        await uow.RollbackAsync();
        throw;
    }
    // ===== トランザクション境界終了 =====
}
```

---

## 検討したパターン：ActionScope

### 概要

**スコープベースで自動的にトランザクションを管理する実験的パターン**

```csharp
// トランザクション管理が暗黙的
return await uow.CommandAsync(async ctx =>
{
    await ctx.Orders.CreateAsync(order);
    await ctx.Inventory.UpdateStockAsync(productId, newStock);
    
    // スコープを抜けたら自動Commit
    // 例外発生時は自動Rollback
});
```

### アーキテクチャ

```csharp
public interface IUnitOfWork
{
    /// <summary>
    /// Command（書き込み）操作を実行（自動トランザクション管理）
    /// </summary>
    Task<T> CommandAsync<T>(Func<IUnitOfWorkContext, Task<T>> command);
    
    /// <summary>
    /// Query（読み取り）操作を実行（トランザクションなし）
    /// </summary>
    Task<T> QueryAsync<T>(Func<IUnitOfWorkContext, Task<T>> query);
}

public class UnitOfWork : IUnitOfWork
{
    public async Task<T> CommandAsync<T>(Func<IUnitOfWorkContext, Task<T>> command)
    {
        using var tx = _connection.BeginTransaction();
        
        try
        {
            var context = new UnitOfWorkContext(_connection, tx);
            var result = await command(context);
            
            tx.Commit();  // 自動Commit
            return result;
        }
        catch
        {
            tx.Rollback();  // 自動Rollback
            throw;
        }
    }
}
```

### メリット

1. **Commit/Rollbackが自動**
   - 書き忘れのリスクがない
   - try-catch構造が不要

2. **CQRS命名による意図の明確化**
   - `CommandAsync`: 書き込み操作
   - `QueryAsync`: 読み取り操作

3. **コードが簡潔**
   - トランザクション制御のボイラープレートが不要

### 致命的な問題

#### 問題1: 途中のreturnでもCommitされる

```csharp
return await uow.CommandAsync(async ctx =>
{
    var inventory = await ctx.Inventory.GetByProductIdAsync(productId);
    
    if (inventory.Stock < quantity)
    {
        // ここでreturnするとCommitされてしまう！
        // 意図：Rollbackしたい
        // 実際：Commitされる（データ不整合）
        return new Result { Success = false };
    }
    
    await ctx.Orders.CreateAsync(order);
    return new Result { Success = true };
});
```

**期待する動作**: 在庫不足の場合は何もCommitしたくない
**実際の動作**: `return`で正常終了とみなされ、Commitされる

#### 問題2: ビジネスロジックの表現力が低下

```csharp
// 複雑な分岐を含むビジネスロジック
return await uow.CommandAsync(async ctx =>
{
    if (condition1)
    {
        // このreturnでCommitされる
        return result1;
    }
    
    if (condition2)
    {
        // このreturnでもCommitされる
        return result2;
    }
    
    // 本来はCommitすべき処理
    await ctx.Orders.CreateAsync(order);
    return result3;
});
```

すべての`return`でCommitされるため、「一部の分岐ではRollbackしたい」という表現ができない。

#### 問題3: デバッグが困難

```csharp
// スタックトレースが深くなる
return await uow.CommandAsync(async ctx =>  // ← ラムダ1
{
    return await ProcessOrderAsync(ctx, order);  // ← ラムダ2
});

private async Task<int> ProcessOrderAsync(IUnitOfWorkContext ctx, Order order)
{
    return await ValidateAndCreateAsync(ctx, order);  // ← ラムダ3
}
```

エラー発生時のスタックトレースが複雑になり、問題箇所の特定が難しい。

---

## なぜBasic UoWを選んだのか

### 1. レガシーコードへの導入が容易

**ActionScope**
```csharp
// ラムダ式のスコープ管理が必要
return await uow.CommandAsync(async ctx =>
{
    // 既存のコードをここに移植する必要がある
});
```

**Basic UoW**
```csharp
// 既存のtry-catch構造をそのまま活用できる
uow.BeginTransaction();
try
{
    // 既存のコードをほぼそのまま使える
    await uow.CommitAsync();
}
catch
{
    await uow.RollbackAsync();
    throw;
}
```

### 2. 愚直で分かりやすい

**ActionScope**: トランザクション制御が暗黙的
- Commitがどこで発生するか追いにくい
- デバッグ時に混乱しやすい

**Basic UoW**: トランザクション制御が明示的
- Commit/Rollbackが明示されているため、動作が追いやすい
- 新しいメンバーでも理解できる

### 3. ビジネスロジックの表現力が高い

**ActionScope**: 途中のreturnがCommitになる
```csharp
return await uow.CommandAsync(async ctx =>
{
    if (invalidCondition)
        return errorResult;  // Commitされる（意図しない）
    
    await ctx.Orders.CreateAsync(order);
    return successResult;
});
```

**Basic UoW**: 明示的にRollback可能
```csharp
uow.BeginTransaction();
try
{
    if (invalidCondition)
    {
        await uow.RollbackAsync();  // 意図通りRollback
        return errorResult;
    }
    
    await uow.Orders.CreateAsync(order);
    await uow.CommitAsync();
    return successResult;
}
catch
{
    await uow.RollbackAsync();
    throw;
}
```

### 4. 実績のあるパターン

- try-catch構造は.NETの標準的なパターン
- 多くの開発者が慣れ親しんでいる
- エッジケースの対処方法が確立されている

---

## アーキテクチャ詳細

### レイヤー構成

```
┌─────────────────────────────────────────┐
│           Presentation Layer            │
│  (Controllers)                          │
│  - HTTPリクエスト/レスポンス処理        │
└─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│          Application Layer              │
│  (Services)                             │
│  - トランザクション境界の定義           │
│  - 複数Repositoryの組み合わせ           │
│  - ビジネスロジックの実装               │
└─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│           Domain Layer                  │
│  (Entities, ValueObjects, Exceptions)   │
│  - ドメインモデル                       │
│  - ビジネスルール                       │
└─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│        Infrastructure Layer             │
│  (UnitOfWork, Repositories, Database)   │
│  - データアクセス                       │
│  - トランザクション管理の実装           │
└─────────────────────────────────────────┘
```

### 責務の分離

| レイヤー | 責務 | トランザクション管理 |
|---------|------|---------------------|
| **Controller** | HTTPリクエスト/レスポンス処理 | ❌ 関知しない |
| **Service** | ビジネスロジック + トランザクション境界 | ✅ Begin/Commit/Rollback |
| **Repository** | データアクセスロジック | ❌ Transactionを受け取るのみ |
| **UnitOfWork** | Connection/Transaction管理 | ✅ 生成・提供 |

---

## トレードオフ

### Basic UoW パターン

| メリット | デメリット |
|---------|----------|
| ✅ レガシーコードに導入しやすい | ⚠️ Commit/Rollback漏れのリスク |
| ✅ トランザクション制御が明示的 | ⚠️ try-catch構造が冗長 |
| ✅ デバッグが容易 | ⚠️ ボイラープレートが多い |
| ✅ ビジネスロジックの表現力が高い | |
| ✅ チーム全員が理解できる | |

### ActionScope パターン

| メリット | デメリット |
|---------|----------|
| ✅ Commit/Rollback自動化 | ❌ 途中のreturnでCommit |
| ✅ コードが簡潔 | ❌ ビジネスロジックの表現力低下 |
| ✅ CQRS命名 | ❌ デバッグが困難 |
| | ❌ 学習コストが高い |

---

## 結論

このプロジェクトでは、**Basic Unit of Work パターン**を採用しました。

理由：
1. レガシーコードベースへの導入が容易
2. トランザクション制御が明示的で理解しやすい
3. ビジネスロジックを柔軟に表現できる
4. チーム全体が理解できるシンプルさ

ActionScope パターンはアイデアとしては優れているものの、**途中のreturnでもCommitされる**という致命的な問題があり、実務での採用を見送りました。
