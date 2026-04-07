using Moq;
using Telemart.Client.Business.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Store.Order;
using Xunit;

//// ReSharper disable ExceptionNotDocumented

namespace Telemart.Client.Tests
{
    public sealed class OrderRulesTests
    {
        private readonly int lockerId = 1;

        private readonly IDictionaries dictionaries;

        private readonly IOrderRules orderRules;

        public OrderRulesTests()
        {
            dictionaries = new Dictionaries.Dictionaries(null);
            Mock<IWebClient> webClientMock = new Mock<IWebClient>();

            webClientMock.Setup(x => x.AuthenticatedEmployee).Returns(() => new EmployeeContextDto { Id = lockerId });

            orderRules = new OrderRules(dictionaries, webClientMock.Object);
        }

        [Theory]
        [InlineData(0)]
        public void CanEditOrderPositiveTest(int orderStatusId)
        {
            OrderViewModel orderViewModel = new OrderViewModel
            {
                State = dictionaries.GetItemById<OrderStatus>(orderStatusId),
                LockerId = lockerId
            };

            bool canBeChanged = orderRules.IsOrderCanBeChanged(orderViewModel);

            Assert.True(canBeChanged);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public void CanEditOrderNegativeTest(int orderStatusId)
        {
            OrderViewModel orderViewModel = new OrderViewModel
            {
                State = dictionaries.GetItemById<OrderStatus>(orderStatusId),
                LockerId = lockerId
            };

            bool canBeChanged = orderRules.IsOrderCanBeChanged(orderViewModel);

            Assert.False(canBeChanged);
        }
    }
}