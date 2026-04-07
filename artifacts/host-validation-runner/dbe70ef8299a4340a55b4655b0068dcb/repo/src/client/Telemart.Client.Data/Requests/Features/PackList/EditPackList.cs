using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.PackList;

namespace Telemart.Client.Data.Requests.Features.PackList
{
    public sealed class EditPackList : UpdateEntityResultRequestBase<PackListDto, PackListEditDto>
    {
        public EditPackList(int id, PackListEditDto dto)
            : base(dto, ApiResources.PackLists, id, "pack_list_edit")
        {
        }
    }
}