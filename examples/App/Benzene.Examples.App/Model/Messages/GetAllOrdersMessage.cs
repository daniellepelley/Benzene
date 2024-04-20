
using Benzene.Examples.App.Data.Pagination;

namespace Benzene.Examples.App.Model.Messages;

public class GetAllOrdersMessage
{
    public PaginationMessage Pagination { get; set; }
}