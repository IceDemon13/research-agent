using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ModuleLayout
{
    public sealed class CreateModuleLayout : CreateEntityResultRequestBase<ModuleLayoutDto, ModuleLayoutCreateDto>
    {
        public CreateModuleLayout(ModuleLayoutCreateDto dto)
            : base(dto, ApiResources.ModuleLayouts)
        {
        }
    }
}