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

    public Task<Customer?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phone)) return Task.FromResult<Customer?>(null);
        return _customers.GetByPhoneAsync(phone.Trim(), cancellationToken);
    }

    public async Task<Customer> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name);
        var phone = Normalize(request.Phone);
        if (phone is not null && await _customers.GetByPhoneAsync(phone, cancellationToken) is not null)
            throw new InvalidOperationException("A customer with this mobile number already exists.");
        return await _customers.AddAsync(new Customer
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Phone = phone,
            Email = Normalize(request.Email)
        }, cancellationToken);
    }

    public async Task<Customer?> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty) throw new ArgumentException("Customer is required.", nameof(id));
        Validate(request.Name);
        var phone = Normalize(request.Phone);
        if (phone is not null)
        {
            var duplicate = await _customers.GetByPhoneAsync(phone, cancellationToken);
            if (duplicate is not null && duplicate.Id != id)
                throw new InvalidOperationException("A customer with this mobile number already exists.");
        }
        return await _customers.UpdateAsync(new Customer
        {
            Id = id,
            Name = request.Name.Trim(),
            Phone = phone,
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
