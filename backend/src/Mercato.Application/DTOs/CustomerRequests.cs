namespace Mercato.Application.DTOs;

public sealed record CreateCustomerRequest(string Name, string? Phone, string? Email);
public sealed record UpdateCustomerRequest(string Name, string? Phone, string? Email);
