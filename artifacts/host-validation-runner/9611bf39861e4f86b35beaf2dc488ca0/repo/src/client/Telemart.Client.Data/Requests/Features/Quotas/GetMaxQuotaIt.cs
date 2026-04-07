using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Quotas;

namespace Telemart.Client.Data.Requests.Features.Quotas
{
    public sealed class GetMaxQuotaIt : QueryEntityRequestBase<MaxQuotaIt>
    {
        public GetMaxQuotaIt()
            : base(ApiResources.Quotas, "max_quota_it")
        {
        }
    }
}