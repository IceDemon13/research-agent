using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class RtOrders : CallActionWithBodyRequestBase<Result<OrdersCreateRtResponse>, OrdersCreateRtRequest>
    {
        public RtOrders(IReadOnlyCollection<int> orderIds)
            : base(new OrdersCreateRtRequest { OrderIds = orderIds.ToArray() }, ApiResources.Orders, "rt")
        {
        }
    }
}