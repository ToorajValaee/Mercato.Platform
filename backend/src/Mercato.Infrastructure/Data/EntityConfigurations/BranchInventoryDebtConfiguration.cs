using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mercato.Domain.Entities;

namespace Mercato.Infrastructure.Data.EntityConfigurations;

public sealed class BranchInventoryDebtConfiguration : IEntityTypeConfiguration<BranchInventoryDebt>
{
    public void Configure(EntityTypeBuilder<BranchInventoryDebt> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Quantity).HasPrecision(18, 4);

        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(x => x.FromBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(x => x.ToBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
