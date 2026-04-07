using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Quotas;

namespace Telemart.Client.Data.Requests.Features.Quotas
{
    public class QueryQuotas : QueryEntitiesRequestBase<QuotaDto>
    {
        public QueryQuotas()
            : base(ApiResources.Quotas)
        {
        }
    }
}