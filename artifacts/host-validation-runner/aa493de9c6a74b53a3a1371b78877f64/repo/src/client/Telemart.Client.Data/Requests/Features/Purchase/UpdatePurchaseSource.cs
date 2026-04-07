using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Purchase
{
    public sealed class UpdatePurchaseSource : UpdateEntityRequestBase<PurchaseDto, PurchaseSourceSaveDto>
    {
        public UpdatePurchaseSource(int orderProductId, PurchaseSourceSaveDto dto)
            : base(dto, ApiResources.Purchases, orderProductId, "source")
        {
        }
    }
}