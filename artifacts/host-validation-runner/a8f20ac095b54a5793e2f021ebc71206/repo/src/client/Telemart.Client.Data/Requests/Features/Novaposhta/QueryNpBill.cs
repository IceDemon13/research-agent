using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.NovaposhtaBill;

namespace Telemart.Client.Data.Requests.Features.Novaposhta
{
    public class QueryNpBill : QueryEntityRequestBase<NovaposhtaBillSimpleDto>
    {
        public QueryNpBill(int id)
            : base(ApiResources.Novaposhta, "bill", id)
        {
        }
    }
}