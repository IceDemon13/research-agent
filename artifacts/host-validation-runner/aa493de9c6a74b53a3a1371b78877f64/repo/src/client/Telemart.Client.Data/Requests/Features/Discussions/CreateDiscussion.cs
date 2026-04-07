using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Discussions;

namespace Telemart.Client.Data.Requests.Features.Discussions
{
    public sealed class CreateDiscussion : CreateEntityResultRequestBase<DiscussionDto, DiscussionCreateDto>
    {
        public CreateDiscussion(DiscussionCreateDto dto)
            : base(dto, ApiResources.Discussions)
        {
        }
    }
}