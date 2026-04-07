using System;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class UpdateCustomerReceivedOn : CallEntityActionWithBodyRequestResultBase<object, UpdateCustomerReceivedOn.UpdateCustomerReceivedOnDto>
    {
        public UpdateCustomerReceivedOn(int orderId, DateTime? date)
            : base(orderId, new UpdateCustomerReceivedOnDto(date), ApiResources.Orders, "update_received_on")
        {
        }

        public sealed class UpdateCustomerReceivedOnDto
        {
            public UpdateCustomerReceivedOnDto(DateTime? date)
            {
                Date = date;
            }

            [JsonProperty("date")]
            public DateTime? Date { get; init; }
        }
    }
}
