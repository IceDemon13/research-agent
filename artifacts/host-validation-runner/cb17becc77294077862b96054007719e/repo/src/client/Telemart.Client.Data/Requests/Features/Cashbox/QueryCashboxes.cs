using System;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Cashbox;

namespace Telemart.Client.Data.Requests.Features.Cashbox
{
    public sealed class QueryCashboxes : QueryEntitiesRequestBase<CashboxDto>
    {
        public QueryCashboxes()
            : base(ApiResources.Cashboxes)
        {
        }

        public QueryCashboxes(DateTime? modifiedOnAfter)
            : base(new ModifiedOnAfterFilteringItem { ModifiedOnAfter = modifiedOnAfter }, ApiResources.Cashboxes)
        {
        }
    }
}