namespace Mercato.Application.DTOs;

public sealed record ReceiptLine(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);
