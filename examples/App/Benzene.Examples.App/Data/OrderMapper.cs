using Benzene.Examples.App.Model;

namespace Benzene.Examples.App.Data;

public static class OrderMapper
{
    public static OrderDto AsOrderDto(this Order source)
    {
        return new OrderDto
        {
            Id = source.Id,
            Status = source.Status,
            Name = source.Name,
        };
    }

    public static Order AsOrder(this OrderDto source)
    {
        return new Order
        {
            Id = source.Id,
            Status = source.Status,
            Name = source.Name,
        };
    }
}