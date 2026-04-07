using System;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Category
{
    public sealed class QueryCategories : QueryEntitiesPagedRequestBase<CategoryDto>
    {
        public QueryCategories()
            : base(ApiResources.Categories)
        {
        }

        public QueryCategories(DateTime? modifiedOnAfter)
            : base(new ModifiedOnAfterFilteringItem { ModifiedOnAfter = modifiedOnAfter }, ApiResources.Categories)
        {
        }
    }
}