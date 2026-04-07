using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.PackList;

namespace Telemart.Client.Data.Requests.Features.PackList
{
    public sealed class PackListTransferPack : CallEntityActionRequestResultBase<PackListDto>
    {
        public PackListTransferPack(int packListId, int collectorEmployeeId)
            : base(packListId, ApiResources.PackLists, $"transfer_pack/{collectorEmployeeId}")
        {
        }
    }
}