using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalServiceProduct.Actions
{
    public sealed class DefectAdditionalServiceProduct : CallEntityActionWithBodyRequestResultBase<AdditionalServiceProductDefectResultDto, DefectAdditionalServiceProduct.DefectAdditionalServiceProductDto>
    {
        public DefectAdditionalServiceProduct(int additionalServiceProductId, string statedDefect, bool createDiscount, bool createCall)
            : base(additionalServiceProductId, new DefectAdditionalServiceProductDto(additionalServiceProductId, statedDefect, createDiscount, createCall), ApiResources.AdditionalServicesProducts, "defect")
        {
        }

        public class DefectAdditionalServiceProductDto
        {
            public DefectAdditionalServiceProductDto(int additionalServiceProductId, string statedDefect, bool createDiscunt, bool createCall)
            {
                AdditionalServiceProductId = additionalServiceProductId;
                StatedDefect = statedDefect;
                CreateDiscount = createDiscunt;
                CreateCall = createCall;
            }

            [JsonProperty("additional_service_product_id")]
            public int AdditionalServiceProductId { get; }

            [JsonProperty("stated_defect")]
            public string StatedDefect { get; }

            [JsonProperty("create_discount")]
            public bool? CreateDiscount { get; }

            [JsonProperty("create_call")]
            public bool CreateCall { get; }
        }
    }
}