using System;
using System.Collections.Generic;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Content.ProductFeatureGroups
{
    public sealed class FeatureFilteringItem : IFilteringItem
    {
        public FeatureFilteringItem(int categoryId)
        {
            CategoryId = categoryId;
        }

        public int CategoryId { get; }

        public IEnumerable<(string Name, object Value)> BuildParameters()
        {
            yield return new("cat_id", CategoryId);
        }
    }
}