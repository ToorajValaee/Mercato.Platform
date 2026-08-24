namespace Mercato.Domain.Entities;

public class Artist
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
}
