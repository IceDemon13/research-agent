using System.Collections.Generic;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class MergeOrdersParameter
    {
        public MergeOrdersParameter(OrderDto targetOrder, IEnumerable<OrderDto> ordersToMerge)
        {
            TargetOrder = targetOrder;
            OrdersToMerge = ordersToMerge;
        }

        private MergeOrdersParameter()
        {
        }

        public OrderDto TargetOrder { get; }

        public IEnumerable<OrderDto> OrdersToMerge { get; }
    }
}
