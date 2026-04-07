using System.Collections.Generic;
using Telemart.Client.Common.MvvmEnhancements;

namespace Telemart.Client.ViewModels.Directories.ProductsPrices
{
    public class ViolatorsRrpPriceParameter
    {
        public ViolatorsRrpPriceParameter(
            IReadOnlyCollection<ComboBoxItem> rrpSuppliers,
            IReadOnlyCollection<ComboBoxItem> competitors,
            IReadOnlyCollection<ViolatorsRrpProduct> productPrices)
        {
            RrpSuppliers = rrpSuppliers;
            Competitors = competitors;
            ProductPrices = productPrices;
        }

        public IReadOnlyCollection<ComboBoxItem> RrpSuppliers { get; }

        public IReadOnlyCollection<ComboBoxItem> Competitors { get; }

        public IReadOnlyCollection<ViolatorsRrpProduct> ProductPrices { get; }
    }
}
