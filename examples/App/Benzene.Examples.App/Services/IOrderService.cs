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
    Task<IBenzeneResult<OrderDto[]>> GetAllAsync(PaginationMessage pagination);
    Task<IBenzeneResult<OrderDto>> GetAsync(Guid id);
    Task<IBenzeneResult<OrderDto>> SaveAsync(CreateOrderMessage value);
    Task<IBenzeneResult<OrderDto>> UpdateAsync(UpdateOrderMessage updateOrderMessage);
    Task<IBenzeneResult<Guid>> DeleteAsync(Guid id);
}