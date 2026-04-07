using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.PackList;

namespace Telemart.Client.Data.Requests.Features.PackList
{
    public sealed class UpdatePackListSetting : UpdateEntityResultRequestBase<WarehousePackListSettingDto, SaveWarehousePackListSettingDto>
    {
        public UpdatePackListSetting(int id, SaveWarehousePackListSettingDto dto)
            : base(dto, ApiResources.PackLists, id, "pack_list_settings")
        {
        }
    }
}