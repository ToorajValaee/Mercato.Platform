namespace Mercato.Application.DTOs;

public sealed record CreateBranchTransferRequest(
    Guid SourceBranchId,
    Guid DestinationBranchId,
    Guid ProductId,
    int Quantity);
