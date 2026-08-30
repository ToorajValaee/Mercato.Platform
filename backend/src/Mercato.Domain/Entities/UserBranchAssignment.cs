namespace Mercato.Domain.Entities;

public sealed class UserBranchAssignment
{
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }
}
