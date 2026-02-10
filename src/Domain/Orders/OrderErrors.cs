using Domain.Common.Results;

namespace Domain.Orders;

public static class OrderErrors
{
    public static Error NotFoundByOrderId(int orderId) => Error.NotFound(
        "InventoryErrors.NotFound",
        $"Order not found for orderId: {orderId}");

    public static Error InsufficientStock(int productId, int available, int requested) => Error.Problem(
        code: "Order.InsufficientStock",
        description: $"ProductId={productId}, Available={available}, Requested={requested}");

    public static Error EmptyOrder() => Error.Problem(
        code: "Order.EmptyOrder",
        description: "Order must have at least one item.");
}
