using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Showcase;

namespace Telemart.Client.Data.Requests.Features.Showcase
{
    public class QueryShowcase : QueryEntityRequestBase<ShowcaseDto>
    {
        public QueryShowcase(int id)
            : base(ApiResources.Showcases, id)
        {
        }
    }
}