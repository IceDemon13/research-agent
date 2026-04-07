using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Discussions;

namespace Telemart.Client.Data.Requests.Features.Discussions
{
    public class QueryDiscussionTypeProprties : QueryEntitiesRequestBase<DiscussionTypePropertyDto>
    {
        public QueryDiscussionTypeProprties(int discussionTypeId)
            : base($"{ApiResources.Discussions}/{discussionTypeId}/discussion_type_properties")
        {
        }
    }
}