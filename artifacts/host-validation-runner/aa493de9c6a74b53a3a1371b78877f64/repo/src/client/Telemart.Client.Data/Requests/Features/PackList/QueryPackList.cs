using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.PackList;

namespace Telemart.Client.Data.Requests.Features.PackList
{
    public class QueryPackList : QueryEntityRequestBase<PackListDto>
    {
        public QueryPackList(int id)
            : base(ApiResources.PackLists, id)
        {
        }
    }
}