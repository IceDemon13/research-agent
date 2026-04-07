using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Hashtag.Actions
{
    public sealed class UpdateHashtagsByPhone : CallActionWithBodyRequestResultBase<object, UpdateHashtagsByPhoneDto>
    {
        public UpdateHashtagsByPhone(UpdateHashtagsByPhoneDto dto)
            : base(dto, ApiResources.Customers, "update_hashtags_by_phone")
        {
        }
    }
}