using System;
using System.Linq;
using System.Threading.Tasks;
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
        var connectionString = Environment.GetEnvironmentVariable("MERCATO_TEST_DB_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        var branchId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<MercatoDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using (var setupContext = new MercatoDbContext(options))
        {
            await setupContext.Database.EnsureDeletedAsync();
            await setupContext.Database.EnsureCreatedAsync();
            var setupRepository = new InventoryRepository(setupContext);
            await setupRepository.AddMovementAsync(
                branchId,
                productId,
                5,
                "Concurrency-Seed");
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

        Assert.Single(outcomes.Where(exception => exception is null));
        var rejected = Assert.Single(outcomes.Where(exception => exception is not null));
        var stockException = Assert.IsType<InvalidOperationException>(rejected);
        Assert.Equal("Insufficient stock.", stockException.Message);

        await using var verificationContext = new MercatoDbContext(options);
        var verificationRepository = new InventoryRepository(verificationContext);
        var remaining = await verificationRepository.GetAvailableQuantityAsync(branchId, productId);
        Assert.Equal(1, remaining);
    }
}
