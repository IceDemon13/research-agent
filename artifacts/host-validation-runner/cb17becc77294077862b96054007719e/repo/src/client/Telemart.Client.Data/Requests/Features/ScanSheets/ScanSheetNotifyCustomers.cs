using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ScanSheets
{
    public sealed class ScanSheetNotifyCustomers : CallActionWithBodyRequestResultBase<ScanSheetCreateResponse[], ScanSheetNotifyDto>
    {
        public ScanSheetNotifyCustomers(ScanSheetNotifyDto dto)
            : base(dto, ApiResources.ScanSheets, "notify_customers")
        {
        }
    }
}