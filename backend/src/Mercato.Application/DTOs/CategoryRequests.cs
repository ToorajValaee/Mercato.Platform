namespace Mercato.Application.DTOs;

public sealed record CreateCategoryRequest(string Name, Guid? ParentCategoryId);
public sealed record UpdateCategoryRequest(string Name, Guid? ParentCategoryId);
