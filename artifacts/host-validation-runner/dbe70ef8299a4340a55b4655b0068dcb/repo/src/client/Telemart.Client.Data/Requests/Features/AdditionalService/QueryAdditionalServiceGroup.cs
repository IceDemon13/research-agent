using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalService
{
    public sealed class QueryAdditionalServiceGroup : QueryEntityRequestBase<AdditionalServiceGroupDto>
    {
        public QueryAdditionalServiceGroup(object id)
            : base(ApiResources.AdditionalServicesGroups, id)
        {
        }
    }
}