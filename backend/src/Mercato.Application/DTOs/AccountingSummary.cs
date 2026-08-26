namespace Mercato.Application.DTOs;

public sealed record AccountingSummary(
    decimal GrossSales,
    decimal Refunds,
    decimal NetSales,
    decimal ArtistSettlementPayments,
    decimal NetCashMovement,
    int TransactionCount);
