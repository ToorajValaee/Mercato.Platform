using Mercato.Application.Repositories;
using Mercato.Application.Services;
using Xunit;

namespace Mercato.Application.Tests;

public class InventoryServiceTests
{
    [Fact]
    public async Task GetAvailableQuantity_ShouldReturnRepositoryValue()
    {
        var repository = new FakeInventoryRepository { Available = 10 };
        var service = new InventoryServiceImplementation(repository);

        var result = await service.GetAvailableQuantityAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(10, result);
    }

    [Fact]
    public async Task AdjustStock_ShouldCreateMovement()
    {
        var repository = new FakeInventoryRepository();
        var service = new InventoryServiceImplementation(repository);

        await service.AdjustStockAsync(Guid.NewGuid(), Guid.NewGuid(), 5, "Purchase");

        Assert.Single(repository.Movements);
        Assert.Equal(5, repository.Movements[0].Quantity);
    }

    [Fact]
    public async Task AdjustStock_ShouldRejectInsufficientStock()
    {
        var repository = new FakeInventoryRepository { Available = 2 };
        var service = new InventoryServiceImplementation(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AdjustStockAsync(Guid.NewGuid(), Guid.NewGuid(), -3, "Sale"));
    }

    [Fact]
    public async Task TransferStock_ShouldCreateOutAndInMovements()
    {
        var repository = new FakeInventoryRepository { Available = 10 };
        var service = new InventoryServiceImplementation(repository);

        await service.TransferStockAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3);

        Assert.Equal(2, repository.Movements.Count);
        Assert.Equal("Transfer-Out", repository.Movements[0].Type);
        Assert.Equal("Transfer-In", repository.Movements[1].Type);
    }

    [Fact]
    public async Task AdjustStock_ShouldRejectZeroQuantity()
    {
        var repository = new FakeInventoryRepository();
        var service = new InventoryServiceImplementation(repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AdjustStockAsync(Guid.NewGuid(), Guid.NewGuid(), 0, "Invalid"));
    }

    private sealed class FakeInventoryRepository : IInventoryRepository
    {
        public int Available { get; set; }
        public List<(int Quantity, string Type)> Movements { get; } = new();

        public Task<int> GetAvailableQuantityAsync(Guid branchId, Guid productId)
            => Task.FromResult(Available);

        public Task AddMovementAsync(Guid branchId, Guid productId, int quantity, string movementType)
        {
            Movements.Add((quantity, movementType));
            return Task.CompletedTask;
        }
    }
}
