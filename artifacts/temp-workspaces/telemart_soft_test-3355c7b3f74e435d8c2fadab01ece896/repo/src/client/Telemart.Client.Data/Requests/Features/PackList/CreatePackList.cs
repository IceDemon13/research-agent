using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.PackList;

namespace Telemart.Client.Data.Requests.Features.PackList
{
    public sealed class CreatePackList : CreateEntityResultRequestBase<PackListDto, PackListCreateDto>
    {
        public CreatePackList(PackListCreateDto dto)
            : base(dto, ApiResources.PackLists)
        {
        }
    }
}
