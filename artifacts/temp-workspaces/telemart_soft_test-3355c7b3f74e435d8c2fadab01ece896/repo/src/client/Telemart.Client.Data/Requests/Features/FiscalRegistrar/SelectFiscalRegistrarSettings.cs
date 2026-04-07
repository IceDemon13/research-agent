using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.FiscalRegistrar;

namespace Telemart.Client.Data.Requests.Features.FiscalRegistrar
{
    public sealed class SelectFiscalRegistrarSettings : CallEntityActionWithBodyRequestResultBase<FiscalRegistrarSettingsDto, SelectFiscalRegistrarSettingsDto>
    {
        public SelectFiscalRegistrarSettings(int id, string uniqueDeviceId)
            : base(id, new SelectFiscalRegistrarSettingsDto(id, uniqueDeviceId), $"{ApiResources.Fiscal}/settings", "select")
        {
        }
    }
}