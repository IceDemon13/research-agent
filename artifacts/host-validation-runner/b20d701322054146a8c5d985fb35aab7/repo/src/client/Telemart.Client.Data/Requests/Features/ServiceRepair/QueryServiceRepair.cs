using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRepair
{
    public sealed class QueryServiceRepair : QueryEntityRequestBase<ServiceRepairDto>
    {
        public QueryServiceRepair(int id)
            : base(ApiResources.ServiceRepairs, id)
        {
        }
    }
}