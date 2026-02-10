using Domain.Common.Results;

namespace Domain.Inventory;

public static class InventoryErrors
{
    public static Error NotFoundByProductId(int productId) => Error.NotFound(
        "InventoryErrors.NotFound",
        $"Inventory not found for productId: {productId}");
}
