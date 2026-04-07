using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class CanConfirmOrder : CallEntityActionRequestBase<List<ValidationResultItemDto>>
    {
        public CanConfirmOrder(int id)
            : base(id, ApiResources.Orders, "canconfirm")
        {
        }
    }
}