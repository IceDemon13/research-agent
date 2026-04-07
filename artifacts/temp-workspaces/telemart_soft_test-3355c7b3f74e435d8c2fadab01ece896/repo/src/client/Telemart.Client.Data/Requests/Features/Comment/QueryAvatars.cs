using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Comment;

namespace Telemart.Client.Data.Requests.Features.Comment
{
    public sealed class QueryAvatars : QueryEntitiesRequestBase<AvatarDto>
    {
        public QueryAvatars()
            : base($"{ApiResources.Comments}/avatars")
        {
        }
    }
}