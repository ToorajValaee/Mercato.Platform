namespace Mercato.Application.DTOs;

public record InvoiceDto(Guid Id, Guid? CustomerId, Guid BranchId, decimal TotalAmount, DateTime CreatedAt);
