using System.Collections.Generic;

namespace Telemart.Client.Common.ProductDescription
{
    public interface IProductDescriptionBuilder
    {
        string Build(string mask, IReadOnlyDictionary<string, string> featureValues);
    }
}
