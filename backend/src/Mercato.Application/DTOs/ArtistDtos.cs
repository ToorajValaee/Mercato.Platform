namespace Mercato.Application.DTOs;

public sealed record ArtistDto(Guid Id, string Name, string? Phone);
public sealed record CreateArtistRequest(string Name, string? Phone);
public sealed record UpdateArtistRequest(string Name, string? Phone);
