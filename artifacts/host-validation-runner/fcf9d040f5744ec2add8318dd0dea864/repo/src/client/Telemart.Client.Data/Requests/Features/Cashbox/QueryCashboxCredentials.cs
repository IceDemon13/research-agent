using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Cashbox;

namespace Telemart.Client.Data.Requests.Features.Cashbox
{
    public sealed class QueryCashboxCredentials : QueryEntityRequestBase<List<CashboxCredentialDto>>
    {
        public QueryCashboxCredentials(int cashboxId)
            : base(ApiResources.Cashboxes, cashboxId, "credentials")
        {
        }
    }
}