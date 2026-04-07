using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Tag
{
    public sealed class QueryTagFromats : QueryEntitiesPagedRequestBase<TagFormatDto>
    {
        public QueryTagFromats()
            : base("tags/formats")
        {
        }
    }
}
