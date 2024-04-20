using System;
using System.Threading.Tasks;
using Benzene.Core.Results;
using Benzene.Examples.App.Model;

namespace Benzene.Examples.App.Data;

public interface IOrderDbClient
{
    Task<IClientResult<OrderDto[]>> GetAllAsync(Pagination.Pagination pagination);
    Task<IClientResult> CreateAsync(OrderDto orderDto);
    Task<IClientResult<OrderDto>> GetAsync(Guid id);
    Task<IClientResult<OrderDto>> UpdateAsync(OrderDto orderDto);
    Task<IClientResult<Guid>> DeleteAsync(Guid id);
}