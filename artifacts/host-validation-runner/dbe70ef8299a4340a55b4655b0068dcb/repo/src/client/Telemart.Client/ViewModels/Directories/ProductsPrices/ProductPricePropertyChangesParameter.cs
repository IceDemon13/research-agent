using System.Collections.Generic;
using Telemart.PriceCalculation.Context;

namespace Telemart.Client.ViewModels.Directories.ProductsPrices
{
    public sealed class ProductPricePropertyChangesParameter
    {
        public ProductPricePropertyChangesParameter(IReadOnlyCollection<PropertyChangeDto> propertyChanges, string productName)
        {
            PropertyChanges = propertyChanges;
            ProductName = productName;
        }

        public string ProductName { get; }

        public IReadOnlyCollection<PropertyChangeDto> PropertyChanges { get; }
    }
}