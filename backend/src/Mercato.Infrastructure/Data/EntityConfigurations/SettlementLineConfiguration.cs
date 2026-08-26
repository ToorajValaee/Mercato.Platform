using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mercato.Domain.Entities;

namespace Mercato.Infrastructure.Data.EntityConfigurations;

public class SettlementLineConfiguration : IEntityTypeConfiguration<SettlementLine>
{
    public void Configure(EntityTypeBuilder<SettlementLine> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PurchaseAmount).HasPrecision(18, 2);

        builder.HasOne<Artist>()
            .WithMany()
            .HasForeignKey(x => x.ArtistId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
