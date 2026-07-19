using Benzene.Examples.App.Data;
using Benzene.Examples.App.Data.Pagination;
using Benzene.Examples.App.Model;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.App.Services;
using Benzene.Results;
using Moq;
using Xunit;
using PaginationModel = Benzene.Examples.App.Data.Pagination.Pagination;

namespace Benzene.Examples.App.Test;

/// <summary>
/// Unit tests for <see cref="OrderService"/>, the shared example domain's core orchestration - the
/// piece every host example reuses and none of them tested directly before this. Drives it against
/// a mocked <see cref="IOrderDbClient"/> so the branching (create status-mapping, the three-way
/// update path) is exercised in isolation, without a database or a host.
/// </summary>
public class OrderServiceTest
{
    private readonly Mock<IOrderDbClient> _dbClient = new();
    private readonly OrderService _orderService;

    public OrderServiceTest()
    {
        _orderService = new OrderService(_dbClient.Object);
    }

    [Fact]
    public async Task SaveAsync_DbCreatesSuccessfully_ReturnsCreatedWithAPopulatedOrder()
    {
        _dbClient.Setup(x => x.CreateAsync(It.IsAny<OrderDto>())).ReturnsAsync(BenzeneResult.Created());

        var result = await _orderService.SaveAsync(new CreateOrderMessage { Name = "acme", Status = "new" });

        Assert.Equal(BenzeneResultStatus.Created, result.Status);
        Assert.NotEqual(Guid.Empty, result.Payload.Id);
        Assert.Equal("acme", result.Payload.Name);
        Assert.Equal("new", result.Payload.Status);
    }

    [Fact]
    public async Task SaveAsync_DbFails_PropagatesTheFailureStatus()
    {
        _dbClient.Setup(x => x.CreateAsync(It.IsAny<OrderDto>())).ReturnsAsync(BenzeneResult.ServiceUnavailable());

        var result = await _orderService.SaveAsync(new CreateOrderMessage { Name = "acme", Status = "new" });

        Assert.Equal(BenzeneResultStatus.ServiceUnavailable, result.Status);
    }

    [Fact]
    public async Task UpdateAsync_OrderExists_MutatesItAndReturnsTheUpdateResult()
    {
        var id = Guid.NewGuid();
        var existing = new OrderDto { Id = id, Name = "old", Status = "old" };
        _dbClient.Setup(x => x.GetAsync(id)).ReturnsAsync(BenzeneResult.Ok(existing));
        _dbClient.Setup(x => x.UpdateAsync(It.IsAny<OrderDto>()))
            .ReturnsAsync((OrderDto o) => BenzeneResult.Updated(o));

        var result = await _orderService.UpdateAsync(new UpdateOrderMessage { Id = id.ToString(), Name = "new", Status = "shipped" });

        Assert.Equal(BenzeneResultStatus.Updated, result.Status);
        Assert.Equal("new", result.Payload.Name);
        Assert.Equal("shipped", result.Payload.Status);
        _dbClient.Verify(x => x.UpdateAsync(It.Is<OrderDto>(o => o.Id == id && o.Name == "new")), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_OrderNotFound_ReturnsNotFound_WithoutCallingUpdate()
    {
        var id = Guid.NewGuid();
        _dbClient.Setup(x => x.GetAsync(id)).ReturnsAsync(BenzeneResult.NotFound<OrderDto>());

        var result = await _orderService.UpdateAsync(new UpdateOrderMessage { Id = id.ToString(), Name = "new", Status = "shipped" });

        Assert.Equal(BenzeneResultStatus.NotFound, result.Status);
        _dbClient.Verify(x => x.UpdateAsync(It.IsAny<OrderDto>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_LookupFailsWithAnythingElse_ReturnsServiceUnavailable()
    {
        var id = Guid.NewGuid();
        _dbClient.Setup(x => x.GetAsync(id)).ReturnsAsync(BenzeneResult.ServiceUnavailable<OrderDto>());

        var result = await _orderService.UpdateAsync(new UpdateOrderMessage { Id = id.ToString(), Name = "new", Status = "shipped" });

        Assert.Equal(BenzeneResultStatus.ServiceUnavailable, result.Status);
        _dbClient.Verify(x => x.UpdateAsync(It.IsAny<OrderDto>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_PassesTheMappedPaginationThroughToTheDb()
    {
        PaginationModel? captured = null;
        _dbClient.Setup(x => x.GetAllAsync(It.IsAny<PaginationModel>()))
            .Callback((PaginationModel p) => captured = p)
            .ReturnsAsync(BenzeneResult.Ok(Array.Empty<OrderDto>()));

        await _orderService.GetAllAsync(new PaginationMessage { PageNumber = 2, ItemsPerPage = 25 });

        Assert.NotNull(captured);
        Assert.Equal(2, captured!.PageNumber);
        Assert.Equal(25, captured.ItemsPerPage);
    }

    [Fact]
    public async Task GetAsync_DelegatesToTheDb()
    {
        var id = Guid.NewGuid();
        _dbClient.Setup(x => x.GetAsync(id)).ReturnsAsync(BenzeneResult.Ok(new OrderDto { Id = id }));

        var result = await _orderService.GetAsync(id);

        Assert.Equal(BenzeneResultStatus.Ok, result.Status);
        Assert.Equal(id, result.Payload.Id);
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToTheDb()
    {
        var id = Guid.NewGuid();
        _dbClient.Setup(x => x.DeleteAsync(id)).ReturnsAsync(BenzeneResult.Deleted(id));

        var result = await _orderService.DeleteAsync(id);

        Assert.Equal(BenzeneResultStatus.Deleted, result.Status);
        Assert.Equal(id, result.Payload);
    }
}
