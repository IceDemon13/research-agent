using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Cashbox;

namespace Telemart.Client.Data.Requests.Features.Cashbox.Actions
{
    public class OpenCashboxSession : CallEntityActionRequestResultBase<CashboxDto>
    {
        public OpenCashboxSession(int id)
            : base(id, ApiResources.Cashboxes, "open_session")
        {
        }
    }
}