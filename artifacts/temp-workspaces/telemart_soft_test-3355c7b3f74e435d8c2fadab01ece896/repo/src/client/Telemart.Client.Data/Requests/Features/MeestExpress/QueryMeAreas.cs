using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.MeestExpress;

namespace Telemart.Client.Data.Requests.Features.MeestExpress
{
    public sealed class QueryMeAreas : QueryEntitiesRequestBase<MeAreaDto>
    {
        public QueryMeAreas()
            : base($"{ApiResources.MeestExpress}/{ApiResources.Areas}")
        {
        }
    }
}