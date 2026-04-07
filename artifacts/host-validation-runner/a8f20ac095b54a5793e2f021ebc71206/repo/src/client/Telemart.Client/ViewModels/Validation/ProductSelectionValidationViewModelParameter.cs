using System.Collections.Generic;

namespace Telemart.Client.ViewModels.Validation
{
    public sealed class ProductSelectionValidationViewModelParameter
    {
        public ProductSelectionValidationViewModelParameter(string newBarcode, string productName, IReadOnlyCollection<string> existingBarcodes)
        {
            NewBarcode = newBarcode;
            ProductName = productName;
            ExistingBarcodes = existingBarcodes ?? new List<string>();
        }

        public IReadOnlyCollection<string> ExistingBarcodes { get; }

        public string NewBarcode { get; }

        public string ProductName { get; }
    }
}
