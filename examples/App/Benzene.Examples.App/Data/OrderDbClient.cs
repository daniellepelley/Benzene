// using System;
// using System.Linq;
// using System.Threading.Tasks;
// using Benzene.Core.Results;
// using Benzene.Examples.App.Data.Pagination;
// using Benzene.Examples.App.Model;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Logging;
// using Npgsql;
//
// namespace Benzene.Examples.App.Data;
//
// public class OrderDbClient : IOrderDbClient
// {
//     private readonly IDataContext _dataContext;
//     private readonly ILogger _logger;
//
//     public OrderDbClient(IDataContext dataContext, ILogger<OrderDbClient> logger)
//     {
//         _logger = logger;
//         _dataContext = dataContext;
//     }
//
//     public async Task<IClientResult<OrderDto>> GetAsync(Guid id)
//     {
//         try
//         {
//             var order = await _dataContext.Orders.FirstOrDefaultAsync(x => x.Id == id);
//             if (order == null)
//             {
//                 return ClientResult.NotFound<OrderDto>();
//             }
//             _dataContext.Detach(order);
//             return ClientResult.Ok(order.AsOrderDto());
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Database error");
//             return ClientResult.ServiceUnavailable<OrderDto>();
//         }
//     }
//
//     public async Task<IClientResult<OrderDto>> UpdateAsync(OrderDto orderDto)
//     {
//         try
//         {
//             var order = await _dataContext.Orders.FirstOrDefaultAsync(x => x.Id == orderDto.Id);
//
//             if (order == null)
//             {
//                 return ClientResult.NotFound<OrderDto>();
//             }
//
//             order.Status = orderDto.Status;
//             order.Name = orderDto.Name;
//             await _dataContext.SaveChangesAsync();
//             return ClientResult.Updated(orderDto);
//         }
//         catch (DbUpdateException exception)
//         {
//             var postgresException = exception.InnerException as PostgresException;
//             if (postgresException == null)
//             {
//                 return ClientResult.ServiceUnavailable<OrderDto>();
//             }
//
//             return ClientResult.ServiceUnavailable<OrderDto>();
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Database error");
//             return ClientResult.ServiceUnavailable<OrderDto>();
//         }
//     }
//
//
//     public async Task<IClientResult<Guid>> DeleteAsync(Guid id)
//     {
//         try
//         {
//             var order = await _dataContext.Orders.FirstOrDefaultAsync(x => x.Id == id);
//
//             if (order == null)
//             {
//                 return ClientResult.NotFound<Guid>($"Order {id} not found");
//             }
//
//             _dataContext.Remove(order);
//             await _dataContext.SaveChangesAsync();
//             return ClientResult.Deleted(id);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Database error");
//
//             return ClientResult.ServiceUnavailable<Guid>();
//         }
//     }
//
//     public async Task<IClientResult<OrderDto[]>> GetAllAsync(Pagination.Pagination pagination)
//     {
//         try
//         {
//             var orders = await _dataContext.Orders
//                 .Paginate(x => x.Id, pagination)
//                 .ToArrayAsync();
//                 
//             var orderDtos = orders.Select(OrderMapper.AsOrderDto).ToArray();
//             return ClientResult.Ok(orderDtos);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Database error");
//             return ClientResult.ServiceUnavailable<OrderDto[]>();
//         }
//     }
//
//     public async Task<IClientResult> CreateAsync(OrderDto orderDto)
//     {
//         try
//         {
//             _dataContext.Add(orderDto.AsOrder());
//             await _dataContext.SaveChangesAsync();
//             return ClientResult.Created();
//         }
//         catch (DbUpdateException exception)
//         {
//             var postgresException = exception.InnerException as PostgresException;
//             if (postgresException == null)
//             {
//                 return ClientResult.ServiceUnavailable();
//             }
//
//             return ClientResult.ServiceUnavailable();
//         }
//
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Database error");
//             return ClientResult.ServiceUnavailable();
//         }
//     }
// }