using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Quotas
{
    public sealed class DeleteQuota : DeleteEntityResultRequestBase<object>
    {
        public DeleteQuota(int quotaId)
            : base(ApiResources.Quotas, quotaId)
        {
        }
    }
}