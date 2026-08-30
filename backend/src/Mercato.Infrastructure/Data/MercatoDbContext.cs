using Microsoft.EntityFrameworkCore;
using Mercato.Domain.Entities;
using Mercato.Infrastructure.Data.EntityConfigurations;

namespace Mercato.Infrastructure.Data;

public class MercatoDbContext : DbContext
{
    public MercatoDbContext(DbContextOptions<MercatoDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<BranchTransfer> BranchTransfers => Set<BranchTransfer>();
    public DbSet<SettlementLine> SettlementLines => Set<SettlementLine>();
    public DbSet<ArtistSettlement> ArtistSettlements => Set<ArtistSettlement>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AccountingTransaction> AccountingTransactions => Set<AccountingTransaction>();
    public DbSet<CheckoutIdempotencyRecord> CheckoutIdempotencyRecords => Set<CheckoutIdempotencyRecord>();
    public DbSet<SalesReturn> SalesReturns => Set<SalesReturn>();
    public DbSet<SalesReturnLine> SalesReturnLines => Set<SalesReturnLine>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<BranchInventoryDebt> BranchInventoryDebts => Set<BranchInventoryDebt>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserBranchAssignment> UserBranchAssignments => Set<UserBranchAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new ArtistConfiguration());
        modelBuilder.ApplyConfiguration(new BranchConfiguration());
        modelBuilder.ApplyConfiguration(new InvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new StockMovementConfiguration());
        modelBuilder.ApplyConfiguration(new BranchTransferConfiguration());
        modelBuilder.ApplyConfiguration(new SettlementLineConfiguration());
        modelBuilder.ApplyConfiguration(new ArtistSettlementConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
        modelBuilder.ApplyConfiguration(new AccountingTransactionConfiguration());
        modelBuilder.ApplyConfiguration(new CheckoutIdempotencyRecordConfiguration());
        modelBuilder.ApplyConfiguration(new SalesReturnConfiguration());
        modelBuilder.ApplyConfiguration(new SalesReturnLineConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
        modelBuilder.ApplyConfiguration(new OrderItemConfiguration());
        modelBuilder.ApplyConfiguration(new BranchInventoryDebtConfiguration());

        modelBuilder.Entity<UserBranchAssignment>(entity =>
        {
            entity.HasKey(x => new { x.UserId, x.BranchId });
            entity.HasIndex(x => x.BranchId);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
        modelBuilder.Ignore<object>();
    }
}
