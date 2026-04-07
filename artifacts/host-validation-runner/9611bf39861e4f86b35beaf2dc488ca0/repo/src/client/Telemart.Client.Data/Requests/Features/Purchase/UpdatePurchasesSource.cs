using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Purchase
{
    public sealed class UpdatePurchasesSource : CallActionWithBodyRequestBase<List<SetPurchasesSourceResult>, List<PurchasesSourceSaveDto>>
    {
        public UpdatePurchasesSource(List<PurchasesSourceSaveDto> dtos)
            : base(dtos, ApiResources.Purchases, "sources")
        {
        }
    }
}
