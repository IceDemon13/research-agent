using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Quotas;

namespace Telemart.Client.Data.Requests.Features.Quotas
{
    public sealed class UpdateQuota : UpdateEntityResultRequestBase<QuotaDto, QuotaSaveDto>
    {
        public UpdateQuota(int id, QuotaSaveDto saveDto)
            : base(saveDto, ApiResources.Quotas, id)
        {
        }
    }
}