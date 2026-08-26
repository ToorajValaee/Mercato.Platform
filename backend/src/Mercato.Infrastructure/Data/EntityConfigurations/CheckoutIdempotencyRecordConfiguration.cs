using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mercato.Domain.Entities;

namespace Mercato.Infrastructure.Data.EntityConfigurations;

public sealed class CheckoutIdempotencyRecordConfiguration : IEntityTypeConfiguration<CheckoutIdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<CheckoutIdempotencyRecord> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ResponseJson).IsRequired();
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
    }
}
