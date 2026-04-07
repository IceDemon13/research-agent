using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.FiscalRegistrar;

namespace Telemart.Client.Data.Requests.Features.FiscalRegistrar
{
    public sealed class QueryFiscalRegistrarSettings : QueryEntitiesRequestBase<FiscalRegistrarSettingsDto>
    {
        public QueryFiscalRegistrarSettings(string uniqueDeviceId, bool? selected = null)
            : base(ApiResources.Fiscal, "settings", uniqueDeviceId)
        {
            if (selected != null)
            {
                UrlParameters = new[] { (nameof(selected), (object)selected) };
            }
        }
    }
}