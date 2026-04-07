using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.PackList;

namespace Telemart.Client.Data.Requests.Features.PackList
{
    public sealed class CreatePackListSetting : CreateEntityResultRequestBase<WarehousePackListSettingDto, SaveWarehousePackListSettingDto>
    {
        public CreatePackListSetting(SaveWarehousePackListSettingDto dto)
            : base(dto, $"{ApiResources.PackLists}/pack_list_settings")
        {
        }
    }
}