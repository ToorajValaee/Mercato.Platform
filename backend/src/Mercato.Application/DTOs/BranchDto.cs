namespace Mercato.Application.DTOs;

public sealed record BranchDto(Guid Id, string Name, string? Address);

public sealed record CreateBranchRequest(string Name, string? Address);

public sealed record UpdateBranchRequest(string Name, string? Address);
