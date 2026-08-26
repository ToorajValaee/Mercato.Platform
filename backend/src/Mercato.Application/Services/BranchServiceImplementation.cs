using Mercato.Application.DTOs;
using Mercato.Application.Repositories;
using Mercato.Domain.Entities;

namespace Mercato.Application.Services;

public sealed class BranchServiceImplementation : IBranchService
{
    private readonly IBranchRepository _branches;

    public BranchServiceImplementation(IBranchRepository branches)
    {
        _branches = branches;
    }

    public async Task<IReadOnlyList<BranchDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var branches = await _branches.GetAllAsync(cancellationToken);
        return branches.Select(Map).ToArray();
    }

    public async Task<BranchDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var branch = await _branches.GetAsync(id, cancellationToken);
        return branch is null ? null : Map(branch);
    }

    public async Task<BranchDto> CreateAsync(CreateBranchRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name);
        var branch = await _branches.AddAsync(new Branch
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim()
        }, cancellationToken);
        return Map(branch);
    }

    public async Task<BranchDto?> UpdateAsync(Guid id, UpdateBranchRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name);
        var existing = await _branches.GetAsync(id, cancellationToken);
        if (existing is null) return null;
        existing.Name = request.Name.Trim();
        existing.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        var updated = await _branches.UpdateAsync(existing, cancellationToken);
        return updated is null ? null : Map(updated);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => _branches.DeleteAsync(id, cancellationToken);

    private static void Validate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Branch name is required.", nameof(name));
    }

    private static BranchDto Map(Branch branch) => new(branch.Id, branch.Name, branch.Address);
}
