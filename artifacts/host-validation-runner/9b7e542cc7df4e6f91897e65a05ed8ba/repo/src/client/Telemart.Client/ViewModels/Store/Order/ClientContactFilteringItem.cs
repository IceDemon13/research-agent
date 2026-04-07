using System.Collections.Generic;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class ClientContactFilteringItem : IFilteringItem
    {
        public int? OrderId { get; set; }

        public int? ServiceRequestId { get; set; }

        public IEnumerable<(string Name, object Value)> BuildParameters()
        {
            if (OrderId.HasValue)
            {
                yield return ("order_id", OrderId.Value);
            }

            if (ServiceRequestId.HasValue)
            {
                yield return ("service_request_id", ServiceRequestId.Value);
            }
        }
    }
}