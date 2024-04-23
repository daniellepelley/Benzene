using System;
using System.Threading.Tasks;
using Benzene.Abstractions.Results;
using Benzene.Core.Results;
using Benzene.Examples.App.Data.Pagination;
using Benzene.Examples.App.Model;
using Benzene.Examples.App.Model.Messages;
using Benzene.Results;

namespace Benzene.Examples.App.Services;

public class HardcodedOrderService : IOrderService
{
    public Task<IServiceResult<OrderDto[]>> GetAllAsync(PaginationMessage pagination)
    {
        return ServiceResult.Ok(new []
        {
            new OrderDto
            {
                Id = Guid.NewGuid(),
                Name = "some-name",
                Status = "some-status"
            },
            new OrderDto
            {
                Id = Guid.NewGuid(),
                Name = "some-name",
                Status = "some-status"
            }
        }).AsTask();
    }

    public Task<IServiceResult<OrderDto>> GetAsync(Guid id)
    {
        return ServiceResult.Ok(new 
            OrderDto
            {
                Id = Guid.NewGuid(),
                Name = "some-name",
                Status = "some-status"
            }
        ).AsTask();
    }

    public Task<IServiceResult<OrderDto>> SaveAsync(CreateOrderMessage value)
    {
        return ServiceResult.Ok(new 
            OrderDto
            {
                Id = Guid.NewGuid(),
                Name = "some-name",
                Status = "some-status"
            }
        ).AsTask();
    }

    public Task<IServiceResult<OrderDto>> UpdateAsync(UpdateOrderMessage updateOrderMessage)
    {
        return ServiceResult.Ok(new 
            OrderDto
            {
                Id = Guid.NewGuid(),
                Name = "some-name",
                Status = "some-status"
            }
        ).AsTask();
    }

    public Task<IServiceResult<Guid>> DeleteAsync(Guid id)
    {
        return ServiceResult.Ok(Guid.NewGuid()).AsTask();
    }
}