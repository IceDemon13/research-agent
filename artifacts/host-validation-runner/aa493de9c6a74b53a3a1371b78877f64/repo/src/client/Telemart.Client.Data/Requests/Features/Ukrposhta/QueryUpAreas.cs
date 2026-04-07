using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Ukrposhta;

namespace Telemart.Client.Data.Requests.Features.Ukrposhta
{
    public sealed class QueryUpAreas : QueryEntitiesRequestBase<UpAreaDto>
    {
        public QueryUpAreas()
            : base($"{ApiResources.Ukrposhta}/{ApiResources.Areas}")
        {
        }
    }
}