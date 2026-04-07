using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn
{
    public class LockTradeIn : LockRequestBase<TradeInDto>
    {
        public LockTradeIn(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.TradeIns, id)
        {
        }
    }
}