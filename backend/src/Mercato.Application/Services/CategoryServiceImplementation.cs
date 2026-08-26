using Mercato.Application.DTOs;
using Mercato.Application.Repositories;
using Mercato.Domain.Entities;

namespace Mercato.Application.Services;

public sealed class CategoryServiceImplementation : ICategoryService
{
    private readonly ICategoryRepository _categories;

    public CategoryServiceImplementation(ICategoryRepository categories)
    {
        _categories = categories;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => (await _categories.GetAllAsync(cancellationToken)).Select(Map).ToArray();

    public async Task<CategoryDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _categories.GetAsync(id, cancellationToken);
        return category is null ? null : Map(category);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(null, request.Name, request.ParentCategoryId, cancellationToken);
        return Map(await _categories.AddAsync(new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            ParentId = request.ParentCategoryId
        }, cancellationToken));
    }

    public async Task<CategoryDto?> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _categories.GetAsync(id, cancellationToken);
        if (category is null) return null;
        await ValidateAsync(id, request.Name, request.ParentCategoryId, cancellationToken);
        category.Name = request.Name.Trim();
        category.ParentId = request.ParentCategoryId;
        var updated = await _categories.UpdateAsync(category, cancellationToken);
        return updated is null ? null : Map(updated);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => _categories.DeleteAsync(id, cancellationToken);

    private async Task ValidateAsync(Guid? id, string name, Guid? parentId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required.", nameof(name));
        if (parentId == id)
            throw new ArgumentException("A category cannot be its own parent.", nameof(parentId));
        if (parentId is Guid parent && parent != Guid.Empty && await _categories.GetAsync(parent, cancellationToken) is null)
            throw new InvalidOperationException("Parent category was not found.");
    }

    private static CategoryDto Map(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        ParentCategoryId = category.ParentId
    };
}
