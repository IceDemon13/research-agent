using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.Data.Requests.Features.Catalog.TransferObjects;
using Telemart.Client.TransferObjects.Catalog;

namespace Telemart.Client.Data.Requests.Features.Catalog
{
    public sealed class QueryProductCategories : CallActionWithBodyRequestBase<List<CatalogCategoryDto>, QueryProductCategoriesDto>
    {
        public QueryProductCategories(QueryProductCategoriesDto dto)
            : base(dto, ApiResources.Products, "query_categories")
        {
        }
    }
}