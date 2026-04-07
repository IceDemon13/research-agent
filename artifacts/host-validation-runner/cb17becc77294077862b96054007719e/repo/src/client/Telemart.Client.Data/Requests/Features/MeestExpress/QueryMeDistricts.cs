using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.MeestExpress;
using Telemart.Client.TransferObjects.Novaposhta;

namespace Telemart.Client.Data.Requests.Features.MeestExpress
{
    public class QueryMeDistricts : QueryEntitiesRequestBase<MeDistrictDto>
    {
        public QueryMeDistricts()
            : base($"{ApiResources.MeestExpress}/{ApiResources.Districts}")
        {
        }
    }
}