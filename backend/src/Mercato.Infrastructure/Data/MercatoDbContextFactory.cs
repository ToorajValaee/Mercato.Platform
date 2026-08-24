using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Mercato.Infrastructure.Data;

public class MercatoDbContextFactory : IDesignTimeDbContextFactory<MercatoDbContext>
{
    public MercatoDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MercatoDbContext>()
            .UseNpgsql("Host=localhost;Database=mercato;Username=postgres;Password=postgres")
            .Options;

        return new MercatoDbContext(options);
    }
}
