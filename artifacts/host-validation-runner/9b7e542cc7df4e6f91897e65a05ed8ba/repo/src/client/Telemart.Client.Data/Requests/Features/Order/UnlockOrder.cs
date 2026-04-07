using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class UnlockOrder : UnlockRequestBase<OrderDto>
    {
        public UnlockOrder(int id, bool force = false)
            : base(force, ApiResources.Orders, id)
        {
        }
    }
}
