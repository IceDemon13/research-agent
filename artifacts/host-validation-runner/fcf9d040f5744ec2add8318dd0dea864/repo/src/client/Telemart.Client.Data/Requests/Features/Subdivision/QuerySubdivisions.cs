using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Subdivision
{
    public class QuerySubdivisions : QueryEntitiesRequestBase<SubdivisionDto>
    {
        public QuerySubdivisions()
            : base(ApiResources.Subdivisions)
        {
        }
    }
}
