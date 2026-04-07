using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.OrderDocuments
{
    public sealed class QueryDocumentsByOrders : CallActionWithBodyRequestBase<List<OrderDocumentDto>, QueryDocumentsByOrders.GetOrderDocumentDto>
    {
        public QueryDocumentsByOrders(int[] orderIds, int? typeId)
        : base(new GetOrderDocumentDto(orderIds, typeId), ApiResources.Orders, "documents")
        {
        }

        public sealed class GetOrderDocumentDto
        {
            public GetOrderDocumentDto(int[] orderIds, int? typeId)
            {
                OrderIds = orderIds;
                TypeId = typeId;
            }

            [JsonProperty("order_ids")]
            public int[] OrderIds { get; }

            [JsonProperty("type_id")]
            public int? TypeId { get; }
        }
    }
}