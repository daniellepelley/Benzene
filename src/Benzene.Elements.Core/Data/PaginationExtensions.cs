using System;
using System.Linq;
using System.Linq.Expressions;

namespace Benzene.Elements.Core.Data
{
    public static class PaginationExtensions
    {
        private const int MaxPageSize = 1000000;

        public static IQueryable<T> Paginate<T, TKey>(this IQueryable<T> source, Expression<Func<T, TKey>> orderBy, Pagination pagination)
        {
            return source.OrderBy(orderBy)
                .Skip(pagination.PageNumber * pagination.ItemsPerPage)
                .Take(pagination.ItemsPerPage);
        }

        public static Pagination AsPagination(this PaginationMessage source)
        {
            if (source == null)
            {
                return new Pagination
                {
                    ItemsPerPage = MaxPageSize,
                    PageNumber = 0
                };
            }
            return new Pagination
            {
                ItemsPerPage = GetNumberPerPage(source.ItemsPerPage),
                PageNumber = GetPageNumber(source.PageNumber)
            };
        }

        private static int GetNumberPerPage(int? numberPerPage)
        {
            if (!numberPerPage.HasValue || numberPerPage.Value > MaxPageSize || numberPerPage <= 0)
            {
                return MaxPageSize;
            }

            return numberPerPage.Value;
        }
        private static int GetPageNumber(int? pageNumber)
        {
            if (!pageNumber.HasValue || pageNumber <= 0)
            {
                return 0;
            }

            return pageNumber.Value;
        }
    }
}
