using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalServiceProduct.Actions
{
    public sealed class SetUnavailableDiscountProduct : CallActionWithBodyRequestResultBase<AdditionalServiceProductDto, SetUnavailableDiscountProduct.SetUnavailableDiscountProductDto>
    {
        public SetUnavailableDiscountProduct(int additionalServiceProductId, int discountProductId, int presaleOrderId)
            : base(new SetUnavailableDiscountProductDto(additionalServiceProductId, discountProductId, presaleOrderId), ApiResources.AdditionalServicesProducts, "set_unavailable_discount_product")
        {
        }

        public sealed class SetUnavailableDiscountProductDto
        {
            public SetUnavailableDiscountProductDto(int additionalServiceProductId, int discountProductId, int presaleOrderId)
            {
                AdditionalServiceProductId = additionalServiceProductId;
                DiscountProductId = discountProductId;
                PresaleOrderId = presaleOrderId;
            }

            [JsonProperty("additional_service_product_id")]
            public int AdditionalServiceProductId { get; }


            [JsonProperty("discount_product_id")]
            public int DiscountProductId { get; }

            [JsonProperty("presale_order_id")]
            public int PresaleOrderId { get; set; }
        }
    }
}