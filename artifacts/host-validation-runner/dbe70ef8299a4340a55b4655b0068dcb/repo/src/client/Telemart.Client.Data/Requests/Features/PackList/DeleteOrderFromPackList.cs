using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.PackList;

namespace Telemart.Client.Data.Requests.Features.PackList
{
    public class DeleteOrderFromPackList : DeleteEntityResultRequestBase<PackListDto>
    {
        public DeleteOrderFromPackList(int packListId, int packListOrderId)
            : base(ApiResources.PackLists, packListId, "orders", packListOrderId)
        {
        }
    }
}