using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ModuleLayout
{
    public sealed class UpdateModuleLayout : UpdateEntityRequestBase<ModuleLayoutDto, ModuleLayoutUpdateDto>
    {
        public UpdateModuleLayout(int id, ModuleLayoutUpdateDto dto)
            : base(dto, ApiResources.ModuleLayouts, id)
        {
        }
    }
}