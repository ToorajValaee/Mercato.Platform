using Microsoft.EntityFrameworkCore;
using Mercato.Application.Repositories;
using Mercato.Domain.Entities;
using Mercato.Infrastructure.Data;

namespace Mercato.Infrastructure.Repositories;

public sealed class BranchTransferRepository : IBranchTransferRepository
{
    private readonly MercatoDbContext _context;

    public BranchTransferRepository(MercatoDbContext context)
    {
        _context = context;
    }

    public async Task<BranchTransfer> AddAsync(BranchTransfer transfer, CancellationToken cancellationToken = default)
    {
        _context.BranchTransfers.Add(transfer);
        await _context.SaveChangesAsync(cancellationToken);
        return transfer;
    }

    public async Task<IReadOnlyList<BranchTransfer>> GetAllAsync(
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.BranchTransfers.AsNoTracking().AsQueryable();
        if (branchId is Guid branch && branch != Guid.Empty)
            query = query.Where(x => x.SourceBranchId == branch || x.DestinationBranchId == branch);
        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }
}
