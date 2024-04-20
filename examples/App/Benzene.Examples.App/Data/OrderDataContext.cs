// using System.Linq;
// using System.Threading.Tasks;
// using Benzene.Examples.Aws.Data;
// using Benzene.Examples.Aws.Data.Model;
//
// namespace Benzene.Examples.App.Data;
//
// public class OrderDataContext : IDataContext
// {
//     private readonly DataContext _dataContext;
//
//     public OrderDataContext(DataContext dataContext)
//     {
//         _dataContext = dataContext;
//     }
//
//     public IQueryable<Order> Orders => _dataContext.Orders;
//
//     public async Task SaveChangesAsync()
//     {
//         await _dataContext.SaveChangesAsync();
//     }
//
//     public void Add(Order order)
//     {
//         _dataContext.Add(order);
//     }
//
//     public void Attach(Order order)
//     {
//         _dataContext.Attach(order);
//     }
//
//     public void Remove(Order order)
//     {
//         _dataContext.Remove(order);
//     }
//
//     public void Detach(Order order)
//     {
//         _dataContext.Entry(order).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
//     }
// }