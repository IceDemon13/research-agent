using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Admitad
{
    public class QueryAdProvidersTariffCodes : QueryEntitiesRequestBase<AdProviderTariffCodeDto>
    {
        public QueryAdProvidersTariffCodes()
            : base("adprovider/tariff_codes")
        {
        }
    }
}