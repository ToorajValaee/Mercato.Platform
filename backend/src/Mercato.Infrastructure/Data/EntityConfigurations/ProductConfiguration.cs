using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mercato.Domain.Entities;

namespace Mercato.Infrastructure.Data.EntityConfigurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Sku).HasMaxLength(100);
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.PurchasePrice).HasPrecision(18, 2);
        builder.Property(x => x.SalePrice).HasPrecision(18, 2);

        builder.HasOne<Artist>()
            .WithMany()
            .HasForeignKey(x => x.ArtistId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
