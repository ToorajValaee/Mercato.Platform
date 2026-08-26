using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mercato.Domain.Entities;

namespace Mercato.Infrastructure.Data.EntityConfigurations;

public sealed class ArtistSettlementConfiguration : IEntityTypeConfiguration<ArtistSettlement>
{
    public void Configure(EntityTypeBuilder<ArtistSettlement> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TotalSalesCost).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.ArtistId, x.PeriodFromUtc, x.PeriodToUtc }).IsUnique();
        builder.HasIndex(x => new { x.ArtistId, x.IsPaid });
    }
}
