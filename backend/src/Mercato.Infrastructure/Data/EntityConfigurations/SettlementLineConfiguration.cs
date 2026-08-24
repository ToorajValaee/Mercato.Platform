using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mercato.Domain.Entities;

namespace Mercato.Infrastructure.Data.EntityConfigurations;

public class SettlementLineConfiguration : IEntityTypeConfiguration<SettlementLine>
{
    public void Configure(EntityTypeBuilder<SettlementLine> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
    }
}
