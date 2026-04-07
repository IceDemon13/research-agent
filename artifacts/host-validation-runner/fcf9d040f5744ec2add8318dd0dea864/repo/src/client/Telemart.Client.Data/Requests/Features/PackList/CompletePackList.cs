using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.PackList;

namespace Telemart.Client.Data.Requests.Features.PackList
{
    public class CompletePackList : CallEntityActionRequestResultBase<PackListDto>
    {
        public CompletePackList(int id)
            : base(id, ApiResources.PackLists, "complete")
        {
        }
    }
}