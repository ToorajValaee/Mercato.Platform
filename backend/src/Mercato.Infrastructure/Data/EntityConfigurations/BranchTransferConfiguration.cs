using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mercato.Domain.Entities;

namespace Mercato.Infrastructure.Data.EntityConfigurations;

public class BranchTransferConfiguration : IEntityTypeConfiguration<BranchTransfer>
{
    public void Configure(EntityTypeBuilder<BranchTransfer> builder)
    {
        builder.HasKey(x => x.Id);
    }
}
