using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.WorkPlace
{
    public class CreateWorkPlace : CreateEntityResultRequestBase<WorkPlaceDto, WorkPlaceCreateDto>
    {
        public CreateWorkPlace(WorkPlaceCreateDto dto)
            : base(dto, ApiResources.WorkPlaces)
        {
        }
    }
}