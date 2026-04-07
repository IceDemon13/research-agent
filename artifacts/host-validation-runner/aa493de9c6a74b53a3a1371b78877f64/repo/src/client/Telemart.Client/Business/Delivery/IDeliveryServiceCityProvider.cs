using System.Collections.Generic;
using System.Threading.Tasks;
using Telemart.Client.Business.Delivery.Data;

namespace Telemart.Client.Business.Delivery
{
    public interface IDeliveryServiceCityProvider
    {
        Task<IReadOnlyCollection<DeliveryServiceCity>> GetCitiesAsync(string districtRef, string areaRef = null);
    }
}