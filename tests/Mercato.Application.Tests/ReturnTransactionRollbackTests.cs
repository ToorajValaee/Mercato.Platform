using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mercato.Application.DTOs;
using Mercato.Application.Repositories;
using Mercato.Application.Services;
using Mercato.Domain.Entities;
using Mercato.Infrastructure.Data;
using Mercato.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Mercato.Application.Tests;

public sealed class ReturnTransactionRollbackTests
{
    [Fact]
    public async Task Downstream_Accounting_Failure_Rolls_Back_Entire_Return()
    {
        var options = GetDatabaseOptions();
        if (options is null)
            return;

        await ResetDatabaseAsync(options);

        var branchId = Guid.NewGuid();
        var artistId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        await using (var setup = new MercatoDbContext(options))
        {
            setup.Branches.Add(new Branch { Id = branchId, Name = "Main" });
            setup.Artists.Add(new Artist { Id = artistId, Name = "Artist" });
            setup.Products.Add(new Product
            {
                Id = productId,
                Name = "Artist product",
                Sku = "ROLLBACK-1",
                PurchasePrice = 4m,
                SalePrice = 10m,
                ArtistId = artistId
            });
            setup.Orders.Add(new Order
            {
                Id = orderId,
                BranchId = branchId,
                TotalAmount = 20m,
                Items = new[]
                {
                    new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = orderId,
                        ProductId = productId,
                        Quantity = 2,
                        UnitPrice = 10m
                    }
                }
            });
            setup.Invoices.Add(new Invoice
            {
                Id = invoiceId,
                OrderId = orderId,
                BranchId = branchId,
                TotalAmount = 20m,
                CreatedAt = DateTime.UtcNow
            });
            await setup.SaveChangesAsync();
        }

        await using (var context = new MercatoDbContext(options))
        {
            var unitOfWork = new UnitOfWork(context);
            var orderRepository = new OrderRepository(context);
            var invoiceRepository = new InvoiceRepository(context);
            var returnRepository = new SalesReturnRepository(context);
            var productRepository = new ProductRepository(context);
            var branchRepository = new BranchRepository(context);
            var inventoryRepository = new InventoryRepository(context);
            var inventory = new InventoryServiceImplementation(inventoryRepository, productRepository, branchRepository);
            var paymentRepository = new PaymentRepository(context);
            var settlementRepository = new SettlementRepository(context);
            var failingAccounting = new ThrowingAccountingRepository();
            var settlements = new SettlementServiceImplementation(settlementRepository, failingAccounting, unitOfWork);
            var service = new ReturnServiceImplementation(
                orderRepository,
                invoiceRepository,
                returnRepository,
                productRepository,
                inventory,
                settlements,
                paymentRepository,
                failingAccounting,
                unitOfWork);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReturnAsync(new ReturnRequest
            {
                OrderId = orderId,
                RefundMethod = "Cash",
                Items = new[] { new ReturnItem(productId, 1) }
            }));

            Assert.Equal("Simulated accounting failure.", exception.Message);
        }

        await using var verification = new MercatoDbContext(options);
        Assert.Empty(await verification.SalesReturns.AsNoTracking().ToListAsync());
        Assert.Empty(await verification.SalesReturnLines.AsNoTracking().ToListAsync());
        Assert.Empty(await verification.StockMovements.AsNoTracking().ToListAsync());
        Assert.Empty(await verification.SettlementLines.AsNoTracking().ToListAsync());
        Assert.Empty(await verification.Payments.AsNoTracking().Where(x => x.Type == "Refund").ToListAsync());
        Assert.Empty(await verification.AccountingTransactions.AsNoTracking().ToListAsync());
    }

    private sealed class ThrowingAccountingRepository : IAccountingTransactionRepository
    {
        public Task<AccountingTransaction> AddAsync(AccountingTransaction transaction, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Simulated accounting failure.");

        public Task<IReadOnlyList<AccountingTransaction>> GetAsync(
            Guid? branchId = null,
            DateTime? fromUtc = null,
            DateTime? toUtc = null,
            string? type = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AccountingTransaction>>(Array.Empty<AccountingTransaction>());
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
