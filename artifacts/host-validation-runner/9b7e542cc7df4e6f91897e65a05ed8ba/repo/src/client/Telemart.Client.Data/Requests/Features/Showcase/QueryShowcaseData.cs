using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Showcase;

namespace Telemart.Client.Data.Requests.Features.Showcase
{
    public sealed class QueryShowcaseData : CallActionWithBodyRequestBase<IReadOnlyCollection<ShowcaseDataDto>, IReadOnlyCollection<(int productId, int warehouseId, int categoryId)>>
    {
        public QueryShowcaseData(IReadOnlyCollection<(int productId, int warehouseId, int categoryId)> dto)
            : base(dto, ApiResources.Showcases, "data")
        {
        }
    }
}