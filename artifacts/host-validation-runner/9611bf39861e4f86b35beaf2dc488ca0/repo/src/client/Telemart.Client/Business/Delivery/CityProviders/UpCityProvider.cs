using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telemart.Client.Business.Delivery.Data;
using Telemart.Client.Data.Requests.Features.Ukrposhta;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Ukrposhta;

namespace Telemart.Client.Business.Delivery.CityProviders
{
    internal class UpCityProvider : IDeliveryServiceCityProvider
    {
        private readonly IWebClient webClient;

        public UpCityProvider(IWebClient webClient)
        {
            this.webClient = webClient;
        }

        public async Task<IReadOnlyCollection<DeliveryServiceCity>> GetCitiesAsync(string upDistrictId, string _)
        {
            List<UpCityDto> upCities = await webClient.ExecuteApiRequestAsync(new QueryUpCities(new UpCitiesFilter(upDistrictId is null ? null : int.Parse(upDistrictId))));

            List<DeliveryServiceCity> deliveries = upCities
                .Select(x => new DeliveryServiceCity(
                    x.Id.ToString(),
                    MapFullCityName(x.AreaName, x.DistrictName, x.Name),
                    MapFullCityName(x.AreaNameUkr, x.DistrictNameUkr, x.NameUkr),
                    x.Active,
                    x.UpAreaId.ToString(),
                    x.UpDistrictId.ToString()))
                .ToList();

            return deliveries;
        }

        private static string MapFullCityName(string areaName, string districtName, string cityName) => $"{cityName} ({areaName} обл., {districtName} р-н.)";
    }
}