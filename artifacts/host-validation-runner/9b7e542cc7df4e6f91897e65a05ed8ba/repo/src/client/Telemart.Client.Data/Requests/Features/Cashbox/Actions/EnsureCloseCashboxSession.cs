using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Cashbox;

namespace Telemart.Client.Data.Requests.Features.Cashbox.Actions
{
    public class EnsureCloseCashboxSession : CallEntityActionRequestResultBase<CashboxDto>
    {
        public EnsureCloseCashboxSession(int id)
            : base(id, ApiResources.Cashboxes, "close_telemart_session")
        {
        }
    }
}