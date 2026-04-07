using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Directories.ProductsCatalog
{
    public sealed class ProductCatalogFilteringItem : IFilteringItem
    {
        private const string Separator = ",";

        public string Name { get; set; }

        public List<int> Categories { get; set; }

        public List<int> AvailTypes { get; set; }

        public IEnumerable<(string Name, object Value)> BuildParameters()
        {
            if (!string.IsNullOrWhiteSpace(Name))
            {
                yield return ("name", Name);
            }

            if (Categories != null && Categories.Any())
            {
                yield return ("categories", string.Join(Separator, Categories));
            }

            if (AvailTypes != null && AvailTypes.Any())
            {
                yield return ("avail_types", string.Join(Separator, AvailTypes));
            }
        }
    }
}