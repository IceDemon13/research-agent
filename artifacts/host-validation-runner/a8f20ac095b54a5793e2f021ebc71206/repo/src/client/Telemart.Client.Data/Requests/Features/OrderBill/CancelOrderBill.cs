using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.OrderBill
{
    public class CancelOrderBill : CallEntityActionRequestResultBase<OrderBillDto>
    {
        public CancelOrderBill(int id)
            : base(id, ApiResources.OrderBills, "cancel")
        {
        }
    }
}
