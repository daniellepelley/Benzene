using System;
using System.Threading.Tasks;
using Benzene.Abstractions.Results;
using Benzene.Examples.App.Data.Pagination;
using Benzene.Examples.App.Model;
using Benzene.Examples.App.Model.Messages;
using Benzene.Results;

namespace Benzene.Examples.App.Services;

public interface IOrderService
{
    Task<IServiceResult<OrderDto[]>> GetAllAsync(PaginationMessage pagination);
    Task<IServiceResult<OrderDto>> GetAsync(Guid id);
    Task<IServiceResult<OrderDto>> SaveAsync(CreateOrderMessage value);
    Task<IServiceResult<OrderDto>> UpdateAsync(UpdateOrderMessage updateOrderMessage);
    Task<IServiceResult<Guid>> DeleteAsync(Guid id);
}