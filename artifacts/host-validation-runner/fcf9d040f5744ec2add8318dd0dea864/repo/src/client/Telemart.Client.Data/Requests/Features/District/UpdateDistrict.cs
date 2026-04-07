using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.District
{
    public sealed class UpdateDistrict : UpdateEntityRequestBase<Result<DistrictDto>, DistrictDto>
    {
        public UpdateDistrict(int id, DistrictDto dto)
            : base(dto, ApiResources.Districts, id)
        {
        }
    }
}