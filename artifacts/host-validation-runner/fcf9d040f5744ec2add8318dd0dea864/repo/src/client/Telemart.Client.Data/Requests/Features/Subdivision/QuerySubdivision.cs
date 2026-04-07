using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Subdivision
{
    public class QuerySubdivision : QueryEntityRequestBase<SubdivisionDto>
    {
        public QuerySubdivision(int id)
            : base(ApiResources.Subdivisions, id)
        {
        }
    }
}