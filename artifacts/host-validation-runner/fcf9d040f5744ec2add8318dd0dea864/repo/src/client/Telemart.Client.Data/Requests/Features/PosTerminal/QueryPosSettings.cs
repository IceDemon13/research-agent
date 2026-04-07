using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.PosTerminal;

namespace Telemart.Client.Data.Requests.Features.PosTerminal
{
    public sealed class QueryPosSettings : QueryEntitiesRequestBase<PosSettingsDto>
    {
        public QueryPosSettings(string uniqueDeviceId)
            : base($"{ApiResources.Pos}/settings/{uniqueDeviceId}")
        {
        }
    }
}