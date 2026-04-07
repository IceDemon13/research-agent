using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Content;

namespace Telemart.Client.Data.Requests.Features.Products.Content
{
    public sealed class QueryProductFeatures : QueryEntityRequestBase<FeaturesResponse>
    {
        public QueryProductFeatures(int categoryId, IReadOnlyCollection<int> availTypes)
            : base(ApiResources.Features, "products")
        {
            UrlParameters = GetParameters(categoryId, availTypes);
        }

        private static IEnumerable<(string, object)> GetParameters(int categoryId, IReadOnlyCollection<int> availTypes)
        {
            yield return ("cat_id", categoryId);

            if (availTypes?.Any() == true)
            {
                yield return ("avail_types", string.Join(",", availTypes));
            }
        }
    }
}