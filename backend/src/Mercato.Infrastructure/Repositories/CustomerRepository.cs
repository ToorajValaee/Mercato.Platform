using Microsoft.EntityFrameworkCore;
using Mercato.Application.Repositories;
using Mercato.Domain.Entities;
using Mercato.Infrastructure.Data;

namespace Mercato.Infrastructure.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly MercatoDbContext _context;

    public CustomerRepository(MercatoDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Customers.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public Task<Customer?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Customer?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        var normalized = phone.Trim();
        return _context.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Phone == normalized, cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Customers.AnyAsync(x => x.Id == id, cancellationToken);

    public async Task<Customer> AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);
        return customer;
    }

    public async Task<Customer?> UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Customers.FirstOrDefaultAsync(x => x.Id == customer.Id, cancellationToken);
        if (entity is null) return null;
        entity.Name = customer.Name;
        entity.Phone = customer.Phone;
        entity.Email = customer.Email;
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }
}
