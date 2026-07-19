using Benzene.Examples.App.Data;
using Benzene.Examples.App.Model;
using Xunit;

namespace Benzene.Examples.App.Test;

public class OrderMapperTest
{
    [Fact]
    public void AsOrderDto_CopiesEveryField()
    {
        var id = Guid.NewGuid();
        var order = new Order { Id = id, Name = "acme", Status = "shipped" };

        var dto = order.AsOrderDto();

        Assert.Equal(id, dto.Id);
        Assert.Equal("acme", dto.Name);
        Assert.Equal("shipped", dto.Status);
    }

    [Fact]
    public void AsOrder_CopiesEveryField()
    {
        var id = Guid.NewGuid();
        var dto = new OrderDto { Id = id, Name = "acme", Status = "shipped" };

        var order = dto.AsOrder();

        Assert.Equal(id, order.Id);
        Assert.Equal("acme", order.Name);
        Assert.Equal("shipped", order.Status);
    }

    [Fact]
    public void RoundTrip_PreservesEveryField()
    {
        var original = new OrderDto { Id = Guid.NewGuid(), Name = "acme", Status = "shipped" };

        var roundTripped = original.AsOrder().AsOrderDto();

        Assert.Equal(original.Id, roundTripped.Id);
        Assert.Equal(original.Name, roundTripped.Name);
        Assert.Equal(original.Status, roundTripped.Status);
    }
}
