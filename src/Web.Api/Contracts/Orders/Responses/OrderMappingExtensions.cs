using Domain.Orders;

namespace Web.Api.Contracts.Orders.Responses;

public static class OrderMappingExtensions
{
    public static OrderResponse ToResponse(this Order order) =>
        new(
            order.Id,
            order.CustomerId,
            order.CreatedAt,
            order.TotalAmount,
            order.Details.Select(d => d.ToResponse()));

    public static OrderDetailResponse ToResponse(this OrderDetail detail) =>
        new(
            detail.ProductId,
            detail.Quantity,
            detail.UnitPrice,
            detail.SubTotal);
}
