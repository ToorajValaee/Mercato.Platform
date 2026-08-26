using Mercato.Application.DTOs;
using Mercato.Domain.Entities;

namespace Mercato.Application.Services;

public interface ICustomerService
{
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Customer?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Customer> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<Customer?> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);
}
