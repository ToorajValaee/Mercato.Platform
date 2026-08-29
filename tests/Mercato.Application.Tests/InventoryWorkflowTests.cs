using System;
using System.Linq;
using System.Threading.Tasks;
using Mercato.Application.Services;
using Mercato.Domain.Entities;
using Mercato.Infrastructure.Data;
using Mercato.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Mercato.Application.Tests;

public sealed class InventoryWorkflowTests
{
    [Fact]
    public async Task Movements_Reconcile_To_Available_Quantity()
    {
        var options = GetDatabaseOptions();
        if (options is null)
            return;

        await ResetDatabaseAsync(options);
        var branchId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedReferencesAsync(options, branchId, productId);

        await using var context = new MercatoDbContext(options);
        var service = CreateInventoryService(context);

        await service.AdjustStockAsync(productId, branchId, 10, "Opening stock");
        await service.AdjustStockAsync(productId, branchId, -3, "Sale");
        await service.AdjustStockAsync(productId, branchId, 1, "Return");

        var movements = await service.GetMovementsAsync(branchId, productId);
        var available = await service.GetAvailableQuantityAsync(productId, branchId);

        Assert.Equal(8, available);
        Assert.Equal(8m, movements.Sum(x => x.Quantity));
        Assert.Equal(new[] { 10m, -3m, 1m }, movements.OrderBy(x => x.CreatedAtUtc).Select(x => x.Quantity).ToArray());
    }

    [Fact]
    public async Task Transfer_Is_Balanced_Between_Source_And_Destination()
    {
        var options = GetDatabaseOptions();
        if (options is null)
            return;

        await ResetDatabaseAsync(options);
        var sourceId = Guid.NewGuid();
        var destinationId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        await using (var seed = new MercatoDbContext(options))
        {
            seed.Branches.AddRange(
                new Branch { Id = sourceId, Name = "Source" },
                new Branch { Id = destinationId, Name = "Destination" });
            seed.Products.Add(new Product
            {
                Id = productId,
                Name = "Transfer product",
                Sku = "TRANSFER-1",
                PurchasePrice = 4m,
                SalePrice = 10m
            });
            await seed.SaveChangesAsync();
        }

        await using var context = new MercatoDbContext(options);
        var service = CreateInventoryService(context);
        await service.AdjustStockAsync(productId, sourceId, 10, "Opening stock");
        await service.TransferStockAsync(productId, sourceId, destinationId, 4);

        var source = await service.GetAvailableQuantityAsync(productId, sourceId);
        var destination = await service.GetAvailableQuantityAsync(productId, destinationId);
        var transferMovements = (await service.GetMovementsAsync(productId: productId))
            .Where(x => x.Type is "Transfer-Out" or "Transfer-In")
            .ToArray();

        Assert.Equal(6, source);
        Assert.Equal(4, destination);
        Assert.Equal(0m, transferMovements.Sum(x => x.Quantity));
        Assert.Contains(transferMovements, x => x.BranchId == sourceId && x.Quantity == -4m && x.Type == "Transfer-Out");
        Assert.Contains(transferMovements, x => x.BranchId == destinationId && x.Quantity == 4m && x.Type == "Transfer-In");
    }

    private static InventoryServiceImplementation CreateInventoryService(MercatoDbContext context)
        => new(
            new InventoryRepository(context),
            new ProductRepository(context),
            new BranchRepository(context));

    private static async Task SeedReferencesAsync(
        DbContextOptions<MercatoDbContext> options,
        Guid branchId,
        Guid productId)
    {
        await using var context = new MercatoDbContext(options);
        context.Branches.Add(new Branch { Id = branchId, Name = "Main" });
        context.Products.Add(new Product
        {
            Id = productId,
            Name = "Inventory product",
            Sku = "INVENTORY-1",
            PurchasePrice = 4m,
            SalePrice = 10m
        });
        await context.SaveChangesAsync();
    }

    private static DbContextOptions<MercatoDbContext>? GetDatabaseOptions()
    {
        var connectionString = Environment.GetEnvironmentVariable("MERCATO_TEST_DB_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        return new DbContextOptionsBuilder<MercatoDbContext>()
            .UseNpgsql(connectionString)
            .Options;
    }

    private static async Task ResetDatabaseAsync(DbContextOptions<MercatoDbContext> options)
    {
        await using var context = new MercatoDbContext(options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }
}
