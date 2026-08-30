namespace Mercato.Domain.Entities;

public sealed class DiscountDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Percent";
    public decimal Value { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
