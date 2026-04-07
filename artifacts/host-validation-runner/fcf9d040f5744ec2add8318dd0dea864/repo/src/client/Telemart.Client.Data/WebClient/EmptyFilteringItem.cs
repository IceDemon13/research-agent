using System.Collections.Generic;
using System.Linq;

namespace Telemart.Client.Data.WebClient
{
    internal class EmptyFilteringItem : IFilteringItem
    {
        public IEnumerable<(string, object)> BuildParameters()
        {
            return Enumerable.Empty<(string, object)>();
        }
    }
}