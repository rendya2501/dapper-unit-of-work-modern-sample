namespace Web.Api.Contracts.Inventory.Responses;

public record InventoryResponse(
    int ProductId,
    string ProductName,
    int Stock,
    decimal UnitPrice);
