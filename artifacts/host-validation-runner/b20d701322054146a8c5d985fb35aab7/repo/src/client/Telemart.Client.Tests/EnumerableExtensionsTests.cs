using System.Linq;
using Telemart.Client.Extensions;
using Xunit;

namespace Telemart.Client.Tests
{
    public class EnumerableExtensionsTests
    {
        [Fact]
        public void GroupByTest()
        {
            var data = from row in Enumerable.Range(1, 5)
                from col in Enumerable.Range(1, 10)
                select new { Id = (row * 10) + col, Row = row, Col = col };

            var result = data.GroupByMultiple(x => (int?)x.Row, x => (int?)x.Col, 20, 30);
        }
    }
}