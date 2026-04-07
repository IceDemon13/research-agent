using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telemart.Client.Business.Delivery.Data;
using Telemart.Client.Data.Requests.Features.Uklon;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Business.Delivery.CityProviders
{
    internal class UklonCityProvider : IDeliveryServiceCityProvider
    {
        private readonly IWebClient webClient;

        public UklonCityProvider(IWebClient webClient)
        {
            this.webClient = webClient;
        }

        public async Task<IReadOnlyCollection<DeliveryServiceCity>> GetCitiesAsync(string districtRef, string areaRef)
        {
            List<UklonCityDto> cities = await webClient.ExecuteApiRequestAsync(new QueryUklonCities());

            DeliveryServiceCity[] deliveriesCity = cities.Select(x => new DeliveryServiceCity(x.Id.ToString(), x.Name, null, x.Active, null, null)).ToArray();

            return deliveriesCity;
        }
    }
}