using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Purchase
{
    public sealed class CreateNoProductHistory : CreateEntityResultRequestBase<NoProductHistoryDto, NoProductCreateDto>
    {
        public CreateNoProductHistory(NoProductCreateDto noProductCreate)
            : base(noProductCreate, $"{ApiResources.Purchases}/create_no_product_history")
        {
        }
    }
}