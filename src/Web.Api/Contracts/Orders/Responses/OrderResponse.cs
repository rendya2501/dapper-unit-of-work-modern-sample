namespace Web.Api.Contracts.Orders.Responses;

public record OrderResponse(
    int Id,
    int CustomerId,
    DateTime CreatedAt,
    decimal TotalAmount,
    IEnumerable<OrderDetailResponse> Details);

public record OrderDetailResponse(
    int ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal SubTotal);
