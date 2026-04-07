using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Accessory;

namespace Telemart.Client.Data.Requests.Features.Accessory
{
    public class CreateAccessory : CreateEntityResultRequestBase<AccessoryDto, AccessorySaveDto>
    {
        public CreateAccessory(AccessorySaveDto dto)
            : base(dto, ApiResources.Accessories)
        {
        }
    }
}