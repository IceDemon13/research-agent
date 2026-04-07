using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Cashbox;

namespace Telemart.Client.Data.Requests.Features.Cashbox
{
    public class QueryCashbox : QueryEntityRequestBase<CashboxDto>
    {
        public QueryCashbox(int id)
            : base(ApiResources.Cashboxes, id)
        {
        }
    }
}