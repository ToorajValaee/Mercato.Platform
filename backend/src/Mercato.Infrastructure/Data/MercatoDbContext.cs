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
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
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
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<DiscountDefinition> DiscountDefinitions => Set<DiscountDefinition>();
    public DbSet<ApplicationSetting> ApplicationSettings => Set<ApplicationSetting>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptLine> GoodsReceiptLines => Set<GoodsReceiptLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<object>();

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

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Quantity).HasPrecision(18, 4);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.HasOne<Invoice>().WithMany(x => x.Items).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.MobileNumber).HasMaxLength(40);
            entity.Property(x => x.Role).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => x.MobileNumber).IsUnique().HasFilter("\"MobileNumber\" IS NOT NULL");
        });

        modelBuilder.Entity<UserBranchAssignment>(entity =>
        {
            entity.HasKey(x => new { x.UserId, x.BranchId });
            entity.HasIndex(x => x.BranchId);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<DiscountDefinition>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Value).HasPrecision(18, 2);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<ApplicationSetting>(entity =>
        {
            entity.HasKey(x => x.Key);
            entity.Property(x => x.Key).HasMaxLength(120);
            entity.Property(x => x.Value).HasMaxLength(2000);
        });

        modelBuilder.Entity<GoodsReceipt>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Reference).HasMaxLength(120);
            entity.HasOne<Artist>().WithMany().HasForeignKey(x => x.ArtistId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GoodsReceiptLine>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PurchaseUnitPrice).HasPrecision(18, 2);
            entity.HasOne<GoodsReceipt>().WithMany(x => x.Items).HasForeignKey(x => x.GoodsReceiptId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        base.OnModelCreating(modelBuilder);
    }
}
