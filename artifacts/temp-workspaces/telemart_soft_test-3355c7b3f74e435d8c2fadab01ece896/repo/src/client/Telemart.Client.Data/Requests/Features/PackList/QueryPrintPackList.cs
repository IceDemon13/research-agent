using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.PackList;

namespace Telemart.Client.Data.Requests.Features.PackList
{
    public class QueryPrintPackList : QueryEntityRequestBase<PackListPrintDto>
    {
        public QueryPrintPackList(int id)
            : base(ApiResources.PackLists, id, "print")
        {
        }
    }
}