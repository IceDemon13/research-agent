using System;
using System.Globalization;
using Telemart.Client.Dictionaries;
using Xunit;

//// ReSharper disable ExceptionNotDocumented

namespace Telemart.Client.Tests
{
    public sealed class OrderProductSourceTests
    {
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm";

        [Fact]
        public void NoneSourceEqualsTest()
        {
            NoneOrderProductSource s1 = new NoneOrderProductSource();
            NoneOrderProductSource s2 = new NoneOrderProductSource();
            NoneOrderProductSource s3 = s1;

            Assert.Equal(s1, s2);
            Assert.Equal(s1, s3);
        }

        [Theory]
        [InlineData(1, 1, "2016-01-01 16:00", "2016-01-01 16:00", true)]
        [InlineData(1, 36, "2016-01-01 16:00", "2016-01-01 16:00", false)]
        [InlineData(1, 1, "2016-01-01 16:00", "2016-02-02 21:00", false)]
        public void WarehouseSourceEqualsTest(
            int firstWarehouseId,
            int secondWarehouseId,
            string firstDateTime,
            string secondDateTime,
            bool result)
        {
            WarehouseOrderProductSource s1 = new WarehouseOrderProductSource(
                firstWarehouseId,
                "Warehouse1",
                DateTime.ParseExact(firstDateTime, DateTimeFormat, CultureInfo.InvariantCulture));

            WarehouseOrderProductSource s2 = new WarehouseOrderProductSource(
                secondWarehouseId,
                "Warehouse2",
                DateTime.ParseExact(secondDateTime, DateTimeFormat, CultureInfo.InvariantCulture));

            bool eq = s1.Equals(s2);

            Assert.Equal(result, eq);
        }
    }
}