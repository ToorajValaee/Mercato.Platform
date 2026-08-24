namespace Mercato.Application.DTOs;

public record BranchTransferDto(Guid Id, Guid FromBranchId, Guid ToBranchId, Guid ProductId, decimal Quantity, DateTime CreatedAt);
