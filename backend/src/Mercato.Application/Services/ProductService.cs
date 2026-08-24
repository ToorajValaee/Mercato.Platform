using Mercato.Domain.Entities;

namespace Mercato.Application.Services;

public class ProductService
{
    public Product Create(string name, decimal price)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price
        };
    }
}
