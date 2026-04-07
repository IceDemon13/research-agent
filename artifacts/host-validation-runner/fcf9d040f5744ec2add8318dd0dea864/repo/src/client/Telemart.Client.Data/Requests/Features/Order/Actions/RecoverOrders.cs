using Telemart.Client.Data.Requests.Base.Action;

namespace Telemart.Client.Data.Requests.Features.Order.Actions
{
    public class RecoverOrders : CallActionWithBodyRequestBase<string[], int[]>
    {
        public RecoverOrders(int[] orderIds)
            : base(orderIds, ApiResources.Orders, "recover")
        {
        }
    }
}
