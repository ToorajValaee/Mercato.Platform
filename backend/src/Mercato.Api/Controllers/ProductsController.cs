using Mercato.Application.DTOs;
using Mercato.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercato.Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var products = await _productService.GetProductsAsync(cancellationToken);
        return Ok(products);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateProduct(request.Name, request.PurchasePrice, request.SalePrice);
        if (validation is not null)
        {
            return BadRequest(new { error = validation });
        }

        var product = await _productService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateProduct(request.Name, request.PurchasePrice, request.SalePrice);
        if (validation is not null)
        {
            return BadRequest(new { error = validation });
        }

        var product = await _productService.UpdateAsync(id, request, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Archive(
        Guid id,
        CancellationToken cancellationToken)
    {
        var archived = await _productService.ArchiveAsync(id, cancellationToken);

        if (!archived)
        {
            return NotFound();
        }

        return NoContent();
    }

    private static string? ValidateProduct(string name, decimal purchasePrice, decimal salePrice)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Product name is required.";
        }

        if (purchasePrice < 0)
        {
            return "Purchase price cannot be negative.";
        }

        if (salePrice <= 0)
        {
            return "Sale price must be greater than zero.";
        }

        return null;
    }
}
