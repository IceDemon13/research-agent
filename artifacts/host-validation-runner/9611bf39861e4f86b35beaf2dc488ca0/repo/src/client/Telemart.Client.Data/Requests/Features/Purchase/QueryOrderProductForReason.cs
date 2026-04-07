using System;
using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Purchase
{
    public sealed class QueryOrderProductForReason : QueryEntitiesRequestBase<NoReasonOrderProductDto>
    {
        public QueryOrderProductForReason(int orderId, int productId)
            : base(new GetOrderProductForReason(orderId, productId), $"{ApiResources.Purchases}/reason_order_product")
        {
        }

        public sealed class GetOrderProductForReason : FilteringItemBase
        {
            public GetOrderProductForReason(int orderId, int productId)
            {
                OrderId = orderId;
                ProductId = productId;
            }

            [FilteringItemProperty("order_id")]
            public int OrderId { get; set; }

            [FilteringItemProperty("product_id")]
            public int ProductId { get; set; }
        }
    }
}