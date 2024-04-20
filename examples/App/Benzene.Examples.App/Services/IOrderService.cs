using System;
using System.Threading.Tasks;
using Benzene.Abstractions.Results;
using Benzene.Examples.App.Data.Pagination;
using Benzene.Examples.App.Model;
using Benzene.Examples.App.Model.Messages;

namespace Benzene.Examples.App.Services;

public interface IOrderService
{
    Task<IHandlerResult<OrderDto[]>> GetAllAsync(PaginationMessage pagination);
    Task<IHandlerResult<OrderDto>> GetAsync(Guid id);
    Task<IHandlerResult<OrderDto>> SaveAsync(CreateOrderMessage value);
    Task<IHandlerResult<OrderDto>> UpdateAsync(UpdateOrderMessage updateOrderMessage);
    Task<IHandlerResult<Guid>> DeleteAsync(Guid id);
}