using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    // TODO: Put historyType into dto
    public sealed class QueryOrderHistory : CallEntityActionWithBodyRequestBase<List<AuditEntryDto>, OrderIdentityDto>
    {
        public QueryOrderHistory(int orderId, OrderIdentityDto orderIdentity, OrderHistoryType historyType)
            : base(orderId, orderIdentity, ApiResources.Orders, "history")
        {
            UrlParameters = new (string Name, object Value)[] { ("history_type", historyType) };
        }
    }
}