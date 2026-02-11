namespace Web.Api.Contracts.Inventory.Responses;

public static class InventoryMappingExtensions
{
    public static InventoryResponse ToResponse(this Domain.Inventory.Inventory inventory) =>
        new(
            inventory.ProductId,
            inventory.ProductName,
            inventory.Stock,
            inventory.UnitPrice);
}
