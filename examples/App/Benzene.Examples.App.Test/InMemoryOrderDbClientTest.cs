using Benzene.Examples.App.Data;
using Benzene.Examples.App.Data.Pagination;
using Benzene.Examples.App.Model;
using Benzene.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Benzene.Examples.App.Test;

/// <summary>
/// Behavioural tests for <see cref="InMemoryOrderDbClient"/>. Its store is a <c>static</c>
/// <see cref="System.Collections.Concurrent.ConcurrentBag{T}"/>, so each test clears it first;
/// xunit serializes tests within one class, so no cross-test races here.
/// </summary>
public class InMemoryOrderDbClientTest
{
    private readonly InMemoryOrderDbClient _client = new(NullLogger<InMemoryOrderDbClient>.Instance);

    public InMemoryOrderDbClientTest()
    {
        InMemoryOrderDbClient.Orders.Clear();
    }

    private static OrderDto NewOrder() => new() { Id = Guid.NewGuid(), Name = "acme", Status = "new" };

    [Fact]
    public async Task Create_ThenGet_ReturnsTheStoredOrder()
    {
        var order = NewOrder();

        var createResult = await _client.CreateAsync(order);
        var getResult = await _client.GetAsync(order.Id);

        Assert.Equal(BenzeneResultStatus.Created, createResult.Status);
        Assert.Equal(BenzeneResultStatus.Ok, getResult.Status);
        Assert.Equal(order.Name, getResult.Payload.Name);
    }

    [Fact]
    public async Task Get_MissingOrder_ReturnsNotFound()
    {
        var result = await _client.GetAsync(Guid.NewGuid());

        Assert.Equal(BenzeneResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Update_ExistingOrder_MutatesTheStoredInstance()
    {
        var order = NewOrder();
        await _client.CreateAsync(order);

        var updateResult = await _client.UpdateAsync(new OrderDto { Id = order.Id, Name = "changed", Status = "shipped" });
        var getResult = await _client.GetAsync(order.Id);

        Assert.Equal(BenzeneResultStatus.Updated, updateResult.Status);
        Assert.Equal("changed", getResult.Payload.Name);
        Assert.Equal("shipped", getResult.Payload.Status);
    }

    [Fact]
    public async Task Update_MissingOrder_ReturnsNotFound()
    {
        var result = await _client.UpdateAsync(NewOrder());

        Assert.Equal(BenzeneResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Delete_ExistingOrder_RemovesIt()
    {
        var order = NewOrder();
        await _client.CreateAsync(order);

        var deleteResult = await _client.DeleteAsync(order.Id);
        var getResult = await _client.GetAsync(order.Id);

        Assert.Equal(BenzeneResultStatus.Deleted, deleteResult.Status);
        Assert.Equal(BenzeneResultStatus.NotFound, getResult.Status);
    }

    [Fact]
    public async Task Delete_MissingOrder_ReturnsNotFound()
    {
        var result = await _client.DeleteAsync(Guid.NewGuid());

        Assert.Equal(BenzeneResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetAll_ReturnsEveryStoredOrder_WithDefaultPagination()
    {
        await _client.CreateAsync(NewOrder());
        await _client.CreateAsync(NewOrder());
        await _client.CreateAsync(NewOrder());

        var result = await _client.GetAllAsync(new PaginationMessage().AsPagination());

        Assert.Equal(BenzeneResultStatus.Ok, result.Status);
        Assert.Equal(3, result.Payload.Length);
    }

    [Fact]
    public async Task GetAll_HonoursTheItemsPerPageLimit()
    {
        await _client.CreateAsync(NewOrder());
        await _client.CreateAsync(NewOrder());
        await _client.CreateAsync(NewOrder());

        var firstPage = await _client.GetAllAsync(new Pagination { PageNumber = 0, ItemsPerPage = 2 });

        Assert.Equal(2, firstPage.Payload.Length);
    }
}
