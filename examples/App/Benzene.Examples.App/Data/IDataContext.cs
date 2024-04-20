using System.Linq;
using System.Threading.Tasks;

namespace Benzene.Examples.App.Data;

public interface IDataContext
{
    IQueryable<Order> Orders { get; }
    Task SaveChangesAsync();
    void Add(Order order);
    void Attach(Order order);
    void Detach(Order order);
    void Remove(Order order);
}