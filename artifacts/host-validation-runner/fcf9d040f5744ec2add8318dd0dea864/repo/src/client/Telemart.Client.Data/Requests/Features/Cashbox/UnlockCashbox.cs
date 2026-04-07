using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Cashbox;

namespace Telemart.Client.Data.Requests.Features.Cashbox
{
    public class UnlockCashbox : UnlockRequestBase<CashboxDto>
    {
        public UnlockCashbox(int id, bool force = false)
            : base(force, ApiResources.Cashboxes, id)
        {
        }
    }
}