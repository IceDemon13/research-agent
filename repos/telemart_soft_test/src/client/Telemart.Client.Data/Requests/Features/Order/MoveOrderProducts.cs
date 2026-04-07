using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class MoveOrderProducts : CallActionWithBodyRequestResultBase<OrderDto[], MoveOrderProducts.MoveOrderProductsDto>
    {
        public MoveOrderProducts(int sourceOrderId, int targetOrderId, int[] productsToMove)
            : base(new MoveOrderProductsDto(sourceOrderId, targetOrderId, productsToMove), ApiResources.Orders, "move")
        {
        }

        public class MoveOrderProductsDto
        {
            public MoveOrderProductsDto(int sourceOrderId, int targetOrderId, int[] toMove)
            {
                SourceOrderId = sourceOrderId;
                TargetOrderId = targetOrderId;
                ToMove = toMove;
            }

            [JsonProperty("source_order_id")]
            public int SourceOrderId { get; set; }

            [JsonProperty("target_order_id")]
            public int TargetOrderId { get; set; }

            [JsonProperty("order_products_to_move")]
            public int[] ToMove { get; set; }
        }
    }
}