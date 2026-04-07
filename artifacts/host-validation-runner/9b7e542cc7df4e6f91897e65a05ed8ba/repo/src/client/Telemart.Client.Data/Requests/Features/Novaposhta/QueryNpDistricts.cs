using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Novaposhta;

namespace Telemart.Client.Data.Requests.Features.Novaposhta
{
    public class QueryNpDistricts : QueryEntitiesRequestBase<NpDistrictDto>
    {
        public QueryNpDistricts()
            : base($"{ApiResources.Novaposhta}/{ApiResources.Districts}")
        {
        }
    }
}