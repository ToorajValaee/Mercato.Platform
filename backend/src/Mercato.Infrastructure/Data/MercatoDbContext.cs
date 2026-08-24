using Microsoft.EntityFrameworkCore;
using Mercato.Infrastructure.Data.EntityConfigurations;

namespace Mercato.Infrastructure.Data;

public class MercatoDbContext : DbContext
{
    public MercatoDbContext(DbContextOptions<MercatoDbContext> options) : base(options)
    {
    }

    public DbSet<object> Products => Set<object>();
    public DbSet<object> Categories => Set<object>();
    public DbSet<object> Invoices => Set<object>();
    public DbSet<object> StockMovements => Set<object>();
    public DbSet<object> BranchTransfers => Set<object>();
    public DbSet<object> SettlementLines => Set<object>();
    public DbSet<object> Payments => Set<object>();
    public DbSet<object> Customers => Set<object>();

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
