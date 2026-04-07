using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.Event
{
    public sealed class CreateEvent : CreateEntityRequestBase<Result<EventDto>, EventCreateDto>
    {
        public CreateEvent(EventCreateDto dto)
            : base(dto, ApiResources.Events)
        {
        }
    }
}