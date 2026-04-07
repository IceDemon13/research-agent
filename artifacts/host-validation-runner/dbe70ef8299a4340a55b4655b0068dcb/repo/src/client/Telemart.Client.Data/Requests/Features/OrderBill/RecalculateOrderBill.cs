using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.OrderBill
{
    public class RecalculateOrderBill : CallEntityActionRequestResultBase<OrderBillDto>
    {
        public RecalculateOrderBill(int id)
            : base(id, ApiResources.OrderBills, "recalculate")
        {
        }
    }
}
