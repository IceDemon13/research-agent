using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Content;

namespace Telemart.Client.Data.Requests.Features.FeatureGroup
{
    public class QueryFeatureGroups : QueryEntitiesPagedRequestBase<FeatureGroupDto>
    {
        public QueryFeatureGroups(int? categoryId = null, bool findParentCategory = false)
            : base(new FeaturesGroupsFilteringItem(categoryId, findParentCategory), ApiResources.FeaturesGroups)
        {
        }

        private sealed class FeaturesGroupsFilteringItem : IFilteringItem
        {
            private readonly int? _categoryId;
            private readonly bool _findParentCategory;

            public FeaturesGroupsFilteringItem(int? categoryId, bool findParentCategory)
            {
                _categoryId = categoryId;
                _findParentCategory = findParentCategory;
            }

            public IEnumerable<(string, object)> BuildParameters()
            {
                if (_categoryId.HasValue)
                {
                    yield return ("category_id", _categoryId);
                }

                yield return ("find_parent_category", _findParentCategory);
            }
        }
    }
}