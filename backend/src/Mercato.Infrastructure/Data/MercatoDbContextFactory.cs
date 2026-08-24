using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Mercato.Infrastructure.Data;

public class MercatoDbContextFactory : IDesignTimeDbContextFactory<MercatoDbContext>
{
    public MercatoDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MercatoDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("MERCATO_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=mercato;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);

        return new MercatoDbContext(optionsBuilder.Options);
    }
}
