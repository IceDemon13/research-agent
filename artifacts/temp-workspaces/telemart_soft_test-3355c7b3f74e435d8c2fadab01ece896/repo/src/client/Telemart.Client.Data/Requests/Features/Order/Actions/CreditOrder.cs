using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order.Actions
{
    public sealed class CreditOrder : CallEntityActionWithBodyRequestResultBase<object, OrderCreditDataDto>
    {
        public CreditOrder(int id, OrderCreditDataDto dto)
            : base(id, dto, ApiResources.Orders, "credit")
        {
        }
    }
}