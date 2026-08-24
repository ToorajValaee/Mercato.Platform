using Microsoft.EntityFrameworkCore;
using Mercato.Infrastructure.Data.EntityConfigurations;

namespace Mercato.Infrastructure.Data;

public class MercatoDbContext : DbContext
{
    public MercatoDbContext(DbContextOptions<MercatoDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new InvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new StockMovementConfiguration());
        modelBuilder.ApplyConfiguration(new BranchTransferConfiguration());
        modelBuilder.ApplyConfiguration(new SettlementLineConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
