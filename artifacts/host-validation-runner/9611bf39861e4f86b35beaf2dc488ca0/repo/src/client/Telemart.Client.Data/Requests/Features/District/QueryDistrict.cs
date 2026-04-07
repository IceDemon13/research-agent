using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.District
{
    public sealed class QueryDistrict : QueryEntityRequestBase<DistrictDto>
    {
        public QueryDistrict(object id)
        : base(ApiResources.Districts, id)
        {
        }
    }
}