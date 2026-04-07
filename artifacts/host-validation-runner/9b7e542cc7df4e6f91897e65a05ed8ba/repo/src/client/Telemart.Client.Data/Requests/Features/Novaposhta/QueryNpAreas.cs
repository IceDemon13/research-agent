using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Novaposhta;

namespace Telemart.Client.Data.Requests.Features.Novaposhta
{
    public sealed class QueryNpAreas : QueryEntitiesRequestBase<NpAreaDto>
    {
        public QueryNpAreas()
            : base($"{ApiResources.Novaposhta}/{ApiResources.Areas}")
        {
        }
    }
}