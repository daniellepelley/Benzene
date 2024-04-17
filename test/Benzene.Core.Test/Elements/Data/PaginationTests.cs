using Benzene.Elements.Core.Data;
using Xunit;

namespace Benzene.Test.Elements.Data
{
    public class PaginationTests
    {
        private const int MaxPageSize = 1000000;

        [Theory]
        [InlineData(null, null, MaxPageSize, 0)]
        [InlineData(null, 0, MaxPageSize, 0)]
        [InlineData(0, 0, MaxPageSize, 0)]
        [InlineData(-5, -1, MaxPageSize, 0)]
        [InlineData(1, -1, 1, 0)]
        [InlineData(10, -1, 10, 0)]
        [InlineData(101, 1, 101, 1)]
        [InlineData(100, 100, 100, 100)]
        public void AsPaginationTest(int? numberPerPage, int? pageNumber, int expectedNumberPerPage, int expectedPageNumber)
        {
            var paginationMessage = new PaginationMessage
            {
                ItemsPerPage = numberPerPage,
                PageNumber = pageNumber
            };


            var pagination = paginationMessage.AsPagination();

            Assert.Equal(expectedNumberPerPage, pagination.ItemsPerPage);
            Assert.Equal(expectedPageNumber, pagination.PageNumber);
        }
    }
}
