using System;
using System.Threading.Tasks;
using Benzene.Abstractions.Results;
using Benzene.Examples.App.Model;

namespace Benzene.Examples.App.Data;

public interface IOrderDbClient
{
    Task<IBenzeneResult<OrderDto[]>> GetAllAsync(Pagination.Pagination pagination);
    Task<IBenzeneResult> CreateAsync(OrderDto orderDto);
    Task<IBenzeneResult<OrderDto>> GetAsync(Guid id);
    Task<IBenzeneResult<OrderDto>> UpdateAsync(OrderDto orderDto);
    Task<IBenzeneResult<Guid>> DeleteAsync(Guid id);
}