using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.PackList;

namespace Telemart.Client.Data.Requests.Features.PackList
{
    public class QueryInProgressPackList : QueryEntityRequestBase<PackListDto>
    {
        public QueryInProgressPackList()
            : base(ApiResources.PackLists, "in_progress")
        {
        }
    }
}