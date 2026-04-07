using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Prices;

namespace Telemart.Client.Data.Requests.Features.AccountingSystem
{
    public sealed class SaveConversionRatesToAccountingSystem : CallActionWithBodyRequestResultBase<object, SaveRatesRequest>
    {
        public SaveConversionRatesToAccountingSystem(SaveRatesRequest request)
        : base(request, ApiResources.AccountingSystem, "update-rates")
        {
        }
    }
}