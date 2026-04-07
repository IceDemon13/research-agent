using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Locations;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.Locations
{
    public sealed class UpdateLocation : UpdateEntityRequestBase<Result<LocationEntityDto>, LocationEntityDto>
    {
        public UpdateLocation(int id, LocationEntityDto dto)
            : base(dto, ApiResources.Locations, id)
        {
        }
    }
}