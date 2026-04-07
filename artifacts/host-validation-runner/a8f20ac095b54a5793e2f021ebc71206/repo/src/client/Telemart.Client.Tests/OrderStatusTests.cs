using Telemart.Client.Dictionaries;
using Xunit;

//// ReSharper disable ExceptionNotDocumented

namespace Telemart.Client.Tests
{
    public sealed class OrderStatusTests
    {
        [Fact]
        public void ReceivedCompareTest()
        {
            Assert.True(OrderStatus.Received.CompareTo(OrderStatus.Received) == 0);
            Assert.True(OrderStatus.Received.CompareTo(OrderStatus.Confirmed) < 0);
            Assert.True(OrderStatus.Received.CompareTo(OrderStatus.Packed) < 0);
            Assert.True(OrderStatus.Received.CompareTo(OrderStatus.Done) < 0);
            Assert.True(OrderStatus.Received.CompareTo(OrderStatus.Canceled) < 0);
            Assert.True(OrderStatus.Received.CompareTo(OrderStatus.Returned) < 0);
            Assert.True(OrderStatus.Received.CompareTo(OrderStatus.DidNotTake) < 0);
            Assert.True(OrderStatus.Received.CompareTo(OrderStatus.DidNotOrder) < 0);
        }

        [Fact]
        public void ConfirmedCompareTest()
        {
            Assert.True(OrderStatus.Confirmed.CompareTo(OrderStatus.Received) > 0);
            Assert.True(OrderStatus.Confirmed.CompareTo(OrderStatus.Confirmed) == 0);
            Assert.True(OrderStatus.Confirmed.CompareTo(OrderStatus.Packed) < 0);
            Assert.True(OrderStatus.Confirmed.CompareTo(OrderStatus.Done) < 0);
            Assert.True(OrderStatus.Confirmed.CompareTo(OrderStatus.Canceled) < 0);
            Assert.True(OrderStatus.Confirmed.CompareTo(OrderStatus.Returned) < 0);
            Assert.True(OrderStatus.Confirmed.CompareTo(OrderStatus.DidNotTake) < 0);
            Assert.True(OrderStatus.Confirmed.CompareTo(OrderStatus.DidNotOrder) < 0);
        }

        [Fact]
        public void PackedCompareTest()
        {
            Assert.True(OrderStatus.Packed.CompareTo(OrderStatus.Received) > 0);
            Assert.True(OrderStatus.Packed.CompareTo(OrderStatus.Confirmed) > 0);
            Assert.True(OrderStatus.Packed.CompareTo(OrderStatus.Packed) == 0);
            Assert.True(OrderStatus.Packed.CompareTo(OrderStatus.Done) < 0);
            Assert.True(OrderStatus.Packed.CompareTo(OrderStatus.Canceled) < 0);
            Assert.True(OrderStatus.Packed.CompareTo(OrderStatus.Returned) < 0);
            Assert.True(OrderStatus.Packed.CompareTo(OrderStatus.DidNotTake) < 0);
            Assert.True(OrderStatus.Packed.CompareTo(OrderStatus.DidNotOrder) < 0);
        }

        [Fact]
        public void DoneCompareTest()
        {
            Assert.True(OrderStatus.Done.CompareTo(OrderStatus.Received) > 0);
            Assert.True(OrderStatus.Done.CompareTo(OrderStatus.Confirmed) > 0);
            Assert.True(OrderStatus.Done.CompareTo(OrderStatus.Packed) > 0);
            Assert.True(OrderStatus.Done.CompareTo(OrderStatus.Done) == 0);
            Assert.True(OrderStatus.Done.CompareTo(OrderStatus.Canceled) < 0);
            Assert.True(OrderStatus.Done.CompareTo(OrderStatus.Returned) < 0);
            Assert.True(OrderStatus.Done.CompareTo(OrderStatus.DidNotTake) < 0);
            Assert.True(OrderStatus.Done.CompareTo(OrderStatus.DidNotOrder) < 0);
        }

        [Fact]
        public void CanceledReturnedDidNotTakeCompareTest()
        {
            foreach (OrderStatus status in new[] { OrderStatus.Canceled, OrderStatus.Returned, OrderStatus.DidNotTake, OrderStatus.DidNotOrder })
            {
                Assert.True(status.CompareTo(OrderStatus.Received) > 0);
                Assert.True(status.CompareTo(OrderStatus.Confirmed) > 0);
                Assert.True(status.CompareTo(OrderStatus.Packed) > 0);
                Assert.True(status.CompareTo(OrderStatus.Done) > 0);
                Assert.True(status.CompareTo(OrderStatus.Canceled) == 0);
                Assert.True(status.CompareTo(OrderStatus.Returned) == 0);
                Assert.True(status.CompareTo(OrderStatus.DidNotTake) == 0);
                Assert.True(status.CompareTo(OrderStatus.DidNotOrder) == 0);
            }
        }
    }
}