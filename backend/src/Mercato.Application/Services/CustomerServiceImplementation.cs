using Mercato.Application.DTOs;
using Mercato.Application.Repositories;
using Mercato.Domain.Entities;

namespace Mercato.Application.Services;

public sealed class CustomerServiceImplementation : ICustomerService
{
    private readonly ICustomerRepository _customers;

    public CustomerServiceImplementation(ICustomerRepository customers)
    {
        _customers = customers;
    }

    public Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
        => _customers.GetAllAsync(cancellationToken);

    public Task<Customer?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _customers.GetAsync(id, cancellationToken);

    public Task<Customer> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name);
        return _customers.AddAsync(new Customer
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Phone = Normalize(request.Phone),
            Email = Normalize(request.Email)
        }, cancellationToken);
    }

    public Task<Customer?> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty) throw new ArgumentException("Customer is required.", nameof(id));
        Validate(request.Name);
        return _customers.UpdateAsync(new Customer
        {
            Id = id,
            Name = request.Name.Trim(),
            Phone = Normalize(request.Phone),
            Email = Normalize(request.Email)
        }, cancellationToken);
    }

    private static void Validate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Customer name is required.");
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
