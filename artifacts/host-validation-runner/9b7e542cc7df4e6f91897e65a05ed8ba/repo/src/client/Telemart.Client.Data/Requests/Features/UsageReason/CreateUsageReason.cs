using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.UsageReason
{
    public sealed class CreateUsageReason : CreateEntityRequestBase<UsageReasonDto, UsageReasonCreateDto>
    {
        public CreateUsageReason(UsageReasonCreateDto dto)
            : base(dto, ApiResources.UsageReasons)
        {
        }
    }
}