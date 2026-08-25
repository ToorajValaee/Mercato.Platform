namespace Mercato.Application.DTOs;

public sealed record CheckoutItemRequest(
    Guid ProductId,
    int Quantity
);
