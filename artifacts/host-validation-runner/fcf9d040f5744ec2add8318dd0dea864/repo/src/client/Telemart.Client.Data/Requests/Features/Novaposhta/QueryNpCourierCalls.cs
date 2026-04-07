using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Novaposhta;

namespace Telemart.Client.Data.Requests.Features.Novaposhta
{
    public sealed class QueryNpCourierCalls : QueryEntitiesRequestBase<NpCourierCallSimpleDto>
    {
        public QueryNpCourierCalls(IFilteringItem filteringItem)
            : base(filteringItem, ApiResources.Novaposhta, "courier_call")
        {
        }
    }
}
