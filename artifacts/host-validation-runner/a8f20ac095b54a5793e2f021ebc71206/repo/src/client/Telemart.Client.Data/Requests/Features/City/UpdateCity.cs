using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.City;

namespace Telemart.Client.Data.Requests.Features.City
{
    public class UpdateCity : UpdateEntityResultRequestBase<CityDto, CitySaveDto>
    {
        public UpdateCity(CitySaveDto dto)
            : base(dto, ApiResources.Cities, dto.Id)
        {
        }
    }
}
