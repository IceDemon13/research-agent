using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Accessory;

namespace Telemart.Client.Data.Requests.Features.Accessory
{
    public class UpdateAccessory : UpdateEntityResultRequestBase<AccessoryDto, AccessorySaveDto>
    {
        public UpdateAccessory(AccessorySaveDto dto)
            : base(dto, ApiResources.Accessories, dto.Id)
        {
        }
    }
}