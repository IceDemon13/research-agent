using System.Collections.Generic;

namespace Telemart.Client.ViewModels.Directories.ProductsCatalog
{
    public class ProductsCatalogExcelImportSettings
    {
        public ProductsCatalogExcelImportSettings(
            IReadOnlyDictionary<string, string> allowedColumns,
            IReadOnlyDictionary<string, int> availableWarranties,
            HashSet<int> availableCategories,
            HashSet<string> availableColors,
            IReadOnlyDictionary<string, int> warrantyTypes,
            IReadOnlyDictionary<string, int> productTypes,
            int maxRowsForImport)
        {
            WarrantyTypes = warrantyTypes;
            ProductTypes = productTypes;
            AllowedColumns = allowedColumns;
            AvailableWarranties = availableWarranties;
            AvailableCategories = availableCategories;
            AvailableColors = availableColors;
            MaxRowsForImport = maxRowsForImport;
        }

        public IReadOnlyDictionary<string, string> AllowedColumns { get; }

        public IReadOnlyDictionary<string, int> AvailableWarranties { get; }

        public IReadOnlyDictionary<string, int> WarrantyTypes { get; }

        public IReadOnlyDictionary<string, int> ProductTypes { get; }

        public HashSet<int> AvailableCategories { get; }

        public HashSet<string> AvailableColors { get; }

        public int MaxRowsForImport { get; }
    }
}