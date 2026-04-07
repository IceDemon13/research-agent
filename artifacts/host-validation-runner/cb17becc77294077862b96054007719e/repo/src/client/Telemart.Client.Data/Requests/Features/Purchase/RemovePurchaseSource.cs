using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Purchase
{
    public sealed class RemovePurchaseSource : DeleteEntityResultRequestBase<PurchaseDto>
    {
        public RemovePurchaseSource(int orderProductId)
            : base(ApiResources.Purchases, orderProductId, "source")
        {
        }
    }
}