using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.XtraRichEdit.Commands;
using Telemart.Client.Business.Delivery.Data;
using Telemart.Client.Data.Requests.Features.MeestExpress;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.MeestExpress;

namespace Telemart.Client.Business.Delivery.CityProviders
{
    internal class MeCityProvider : IDeliveryServiceCityProvider
    {
        private readonly IWebClient webClient;

        public MeCityProvider(IWebClient webClient)
        {
            this.webClient = webClient;
        }

        public async Task<IReadOnlyCollection<DeliveryServiceCity>> GetCitiesAsync(string districtRef, string areaRef)
        {
            List<MeCityDto> cities = await webClient.ExecuteApiRequestAsync(new QueryMeCities(new MeCitiesFilter(districtRef, areaRef)));

            List<DeliveryServiceCity> deliveries = cities
                .Select(x => new DeliveryServiceCity(
                    x.Ref,
                    MapFullCityName(x.AreaName, x.DistrictName, x.Name),
                    MapFullCityName(x.AreaNameUa, x.DistrictNameUa, x.NameUa),
                    x.Active,
                    x.AreaId,
                    x.DistrictId))
                .ToList();

            return deliveries;
        }

        private string MapFullCityName(string areaName, string districtName, string cityName) => $"{cityName} ({areaName} обл., {districtName} р-н.)";
    }
}