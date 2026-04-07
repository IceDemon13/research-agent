using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order.Actions
{
    public class OrderSetCustomer : CallEntityActionWithBodyRequestResultBase<OrderDto, OrderSetCustomer.OrderSetCustomerDto>
    {
        public OrderSetCustomer(int orderId, int customerId)
            : base(orderId, new OrderSetCustomerDto(orderId, customerId), ApiResources.Orders, "set_customer")
        {
        }

        public class OrderSetCustomerDto
        {
            public OrderSetCustomerDto(int orderId, int customerId)
            {
                OrderId = orderId;
                CustomerId = customerId;
            }

            [JsonProperty("order_id")]
            public int OrderId { get; set; }

            [JsonProperty("customer_id")]
            public int CustomerId { get; set; }
        }
    }
}
