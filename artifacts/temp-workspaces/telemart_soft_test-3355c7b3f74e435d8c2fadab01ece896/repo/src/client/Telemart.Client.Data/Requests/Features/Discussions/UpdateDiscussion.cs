using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Discussions;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.Discussions
{
    public sealed class UpdateDiscussion : UpdateEntityRequestBase<Result<DiscussionDto>, DiscussionUpdateDto>
    {
        public UpdateDiscussion(int id, DiscussionUpdateDto dto)
            : base(dto, ApiResources.Discussions, id)
        {
        }
    }
}