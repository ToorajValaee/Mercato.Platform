namespace Mercato.Application.DTOs;

public sealed record AccountingSummary(
    decimal GrossSales,
    decimal Refunds,
    decimal NetSales,
    int TransactionCount);
