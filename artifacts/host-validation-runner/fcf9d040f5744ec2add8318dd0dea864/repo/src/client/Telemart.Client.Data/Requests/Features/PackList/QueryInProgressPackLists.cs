using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.PackList;

namespace Telemart.Client.Data.Requests.Features.PackList
{
    public sealed class QueryInProgressPackLists : QueryEntitiesRequestBase<PackListDto>
    {
        public QueryInProgressPackLists(IFilteringItem filteringItem)
            : base(filteringItem, $"{ApiResources.PackLists}/pack_lists_in_progress")
        {
        }
    }
}