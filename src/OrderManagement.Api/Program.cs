using FluentValidation;
using Microsoft.Data.Sqlite;
using OrderManagement.Api.Filters;
using OrderManagement.Api.Middleware;
using OrderManagement.Application.Common;
using OrderManagement.Application.Repositories;
using OrderManagement.Application.Services;
using OrderManagement.Application.Services.Abstractions;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Persistence.Database;
using OrderManagement.Infrastructure.Persistence.Repositories;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// appsettings.json から接続文字列を取得
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// ===== データベース初期化 =====
DatabaseInitializer.Initialize(connectionString);

// DI登録
// Controller + 自前の ValidationFilter
builder.Services.AddControllers(options =>
{
    // グローバルに ValidationFilter を適用
    options.Filters.Add<ValidationFilter>();
});

builder.Services.AddOpenApi();

// FluentValidation（Validator のみ登録）
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ===== Database Session =====
//builder.Services.AddScoped<IDbConnection>(sp =>
//{
//    var connectionString =
//        builder.Configuration.GetConnectionString("DefaultConnection")
//            ?? throw new InvalidOperationException("Connection string not found.");

//    return new SqliteConnection(connectionString);
//});
//builder.Services.AddScoped<DbSessionAccessor>(sp =>
//{
//    var connection = sp.GetRequiredService<IDbConnection>();
//    return new DbSessionAccessor(connection);
//});

// DbSessionAccessor が Connection の所有権を持つ
builder.Services.AddScoped<DbSession>(sp =>
{
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string not found.");

    // Connection を作成して DbSessionAccessor に渡す
    // DbSessionAccessor が Dispose 時に Connection も破棄する
    var connection = new SqliteConnection(connStr);
    return new DbSession(connection);
});

// IDbSessionManager (UnitOfWork用)
builder.Services.AddScoped<IDbSessionManager>(sp => sp.GetRequiredService<DbSession>());
// IDbSession (リポジトリ用)
builder.Services.AddScoped<IDbSession>(sp => sp.GetRequiredService<DbSession>());
// UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Repositories
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Services
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// ミドルウェア（例外ハンドリング用）
app.UseMiddleware<ProblemDetailsMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
