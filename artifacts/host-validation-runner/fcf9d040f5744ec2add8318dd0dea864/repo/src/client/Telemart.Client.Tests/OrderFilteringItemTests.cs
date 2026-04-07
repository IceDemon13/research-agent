using System;
using System.Collections.Generic;
using System.Linq;
using Telemart.Client.ViewModels.Store;
using Xunit;

//// ReSharper disable ExceptionNotDocumented

namespace Telemart.Client.Tests
{
    public class OrderFilteringItemTests
    {
        [Fact]
        public void BuildParametersTest()
        {
            OrderFilteringItem item = new OrderFilteringItem("Jhon Doe", new List<int>());

            item.OrderNumbers = "100";
            item.Cellphone = "0966787887";
            item.OrderCreatedAfter = DateTime.Now;
            item.OrderCreatedBefore = DateTime.Now;
            item.OrderClosedAfter = DateTime.Now;
            item.OrderClosedBefore = DateTime.Now;
            item.PkoBool = true;
            item.RtBool = false;
            item.AnyNewCalls = true;
            item.ChangeOrder = false;

            item.Payments = new List<int>();
            item.Cities = new List<int>();
            item.Contractors = new List<int>();
            item.Warehouses = new List<int>();
            item.Carries = new List<int>();
            item.OrderStatuses = new List<int>();

            (string, object)[] tuples = item.BuildParameters().ToArray();

            Assert.NotNull(tuples);
            Assert.NotEmpty(tuples);
        }
    }
}