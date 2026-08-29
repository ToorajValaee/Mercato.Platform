using System;
using System.Linq;
using System.Threading.Tasks;
using Mercato.Domain.Entities;
using Mercato.Infrastructure.Data;
using Mercato.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Mercato.Application.Tests;

public class InventoryConcurrencyTests
{
    [Fact]
    public async Task Concurrent_Deductions_Cannot_Spend_The_Same_Stock_Twice()
    {
        var options = GetDatabaseOptions();
        if (options is null)
            return;

        var branchId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await ResetDatabaseAsync(options);

        await using (var setupContext = new MercatoDbContext(options))
        {
            var setupRepository = new InventoryRepository(setupContext);
            await setupRepository.AddMovementAsync(branchId, productId, 5, "Concurrency-Seed");
        }

        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<Exception?> DeductAsync()
        {
            await using var context = new MercatoDbContext(options);
            var repository = new InventoryRepository(context);
            await gate.Task;
            try
            {
                await repository.AddMovementAsync(
                    branchId,
                    productId,
                    -4,
                    "Concurrent-Sale",
                    preventNegativeBalance: true);
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        var first = DeductAsync();
        var second = DeductAsync();
        gate.SetResult();

        var outcomes = await Task.WhenAll(first, second);

        Assert.Single(outcomes, exception => exception is null);
        var rejected = Assert.Single(outcomes, exception => exception is not null);
        var stockException = Assert.IsType<InvalidOperationException>(rejected);
        Assert.Equal("Insufficient stock.", stockException.Message);

        await using var verificationContext = new MercatoDbContext(options);
        var verificationRepository = new InventoryRepository(verificationContext);
        var remaining = await verificationRepository.GetAvailableQuantityAsync(branchId, productId);
        Assert.Equal(1, remaining);
    }

    [Fact]
    public async Task Concurrent_Return_Checks_Cannot_Refund_The_Same_Sold_Unit_Twice()
    {
        var options = GetDatabaseOptions();
        if (options is null)
            return;

        var orderId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await ResetDatabaseAsync(options);

        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<Exception?> ReturnOneAsync()
        {
            await using var context = new MercatoDbContext(options);
            await using var transaction = await context.Database.BeginTransactionAsync();
            var repository = new SalesReturnRepository(context);
            await gate.Task;

            try
            {
                var alreadyReturned = await repository.GetReturnedQuantityAsync(
                    orderId,
                    productId,
                    default,
                    serialize: true);
                if (alreadyReturned + 1 > 1)
                    throw new InvalidOperationException("Return quantity exceeds quantity sold.");

                var returnId = Guid.NewGuid();
                await repository.AddAsync(new SalesReturn
                {
                    Id = returnId,
                    OrderId = orderId,
                    BranchId = branchId,
                    TotalAmount = 10m,
                    RefundMethod = "Card",
                    Reference = $"TEST-{returnId:N}"[..25],
                    CreatedAtUtc = DateTime.UtcNow,
                    Items = new[]
                    {
                        new SalesReturnLine
                        {
                            Id = Guid.NewGuid(),
                            SalesReturnId = returnId,
                            ProductId = productId,
                            Quantity = 1,
                            UnitPrice = 10m
                        }
                    }
                });
                await transaction.CommitAsync();
                return null;
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync();
                return exception;
            }
        }

        var first = ReturnOneAsync();
        var second = ReturnOneAsync();
        gate.SetResult();

        var outcomes = await Task.WhenAll(first, second);

        Assert.Single(outcomes, exception => exception is null);
        var rejected = Assert.Single(outcomes, exception => exception is not null);
        var returnException = Assert.IsType<InvalidOperationException>(rejected);
        Assert.Equal("Return quantity exceeds quantity sold.", returnException.Message);

        await using var verificationContext = new MercatoDbContext(options);
        var verificationRepository = new SalesReturnRepository(verificationContext);
        var returned = await verificationRepository.GetReturnedQuantityAsync(orderId, productId);
        Assert.Equal(1, returned);
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
