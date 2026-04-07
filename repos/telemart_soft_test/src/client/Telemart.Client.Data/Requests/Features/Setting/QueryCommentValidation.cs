using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Setting
{
    public sealed class QueryCommentValidation : QueryEntityRequestBase<string>
    {
        public QueryCommentValidation()
            : base(ApiResources.Settings, "comment_validation")
        {
        }
    }
}