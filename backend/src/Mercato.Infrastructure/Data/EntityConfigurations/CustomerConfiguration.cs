using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercato.Infrastructure.Data.EntityConfigurations;

public class CustomerConfiguration : IEntityTypeConfiguration<object>
{
    public void Configure(EntityTypeBuilder<object> builder)
    {
    }
}
