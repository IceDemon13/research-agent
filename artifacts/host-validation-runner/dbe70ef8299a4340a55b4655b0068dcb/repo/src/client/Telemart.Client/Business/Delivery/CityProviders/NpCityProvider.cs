using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telemart.Client.Business.Delivery.Data;
using Telemart.Client.Data.Requests.Features.Novaposhta;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Novaposhta;

namespace Telemart.Client.Business.Delivery.CityProviders
{
    internal class NpCityProvider : IDeliveryServiceCityProvider
    {
        private readonly IWebClient webClient;

        public NpCityProvider(IWebClient webClient)
        {
            this.webClient = webClient;
        }

        public async Task<IReadOnlyCollection<DeliveryServiceCity>> GetCitiesAsync(string districtRef, string areaRef)
        {
            List<NpCityDto> cities = await webClient.ExecuteApiRequestAsync(new QueryNpCities(new NpCitiesFilter(areaRef)));

            List<DeliveryServiceCity> deliveriesCity = cities.Select(x => new DeliveryServiceCity(x.Ref, x.Name, x.NameUa, x.Active, x.AreaRef, null)).ToList();

            return deliveriesCity;
        }
    }
}