using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.FiscalRegistrar;

namespace Telemart.Client.Data.Requests.Features.FiscalRegistrar
{
    public sealed class CreateFiscalRegitrarSettings : CreateEntityResultRequestBase<FiscalRegistrarSettingsDto, CreateFiscalRegistrarSettingsDto>
    {
        public CreateFiscalRegitrarSettings(CreateFiscalRegistrarSettingsDto createDto)
            : base(createDto, $"{ApiResources.Fiscal}/settings")
        {
        }
    }
}