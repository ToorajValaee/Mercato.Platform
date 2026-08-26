namespace Mercato.Application.DTOs;

public sealed record SettlementCalculationRequest(
    Guid ArtistId,
    DateTime PeriodFromUtc,
    DateTime PeriodToUtc);
