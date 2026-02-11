using Application.Services;
using Domain.Inventory;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Contracts.Requests;
using Web.Api.Contracts.Responses;
using Web.Api.Extensions;

namespace Web.Api.Controllers;

/// <summary>
/// 在庫管理APIエンドポイント
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InventoryController(InventoryService inventoryService) : ControllerBase
{
    /// <summary>
    /// すべての在庫を取得します
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Inventory>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        var inventories = await inventoryService.GetAllAsync(cancellationToken);
        return inventories.ToOk();
    }

    /// <summary>
    /// 商品IDを指定して在庫を取得します
    /// </summary>
    [HttpGet("{productId}", Name = nameof(GetByProductIdAsync))]
    [ProducesResponseType(typeof(Inventory), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByProductIdAsync(int productId, CancellationToken cancellationToken)
    {
        var inventory = await inventoryService.GetByProductIdAsync(productId, cancellationToken);
        return inventory.ToOk();
    }

    /// <summary>
    /// 在庫を作成します
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateInventoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await inventoryService.CreateAsync(
            request.ProductName, request.Stock, request.UnitPrice, cancellationToken);

        return result.ToResult(
            productId => CreatedAtRoute(
                nameof(GetByProductIdAsync),
                new { productId },
                new CreateInventoryResponse(productId)));
    }

    /// <summary>
    /// 在庫を更新します
    /// </summary>
    [HttpPut("{productId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(int productId, [FromBody] UpdateInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await inventoryService.UpdateAsync(
            productId, request.ProductName, request.Stock, request.UnitPrice, cancellationToken);

        return result.ToNoContent();
    }

    /// <summary>
    /// 在庫を削除します
    /// </summary>
    [HttpDelete("{productId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(int productId, CancellationToken cancellationToken)
    {
        var result = await inventoryService.DeleteAsync(productId, cancellationToken);

        return result.ToNoContent();
    }
}