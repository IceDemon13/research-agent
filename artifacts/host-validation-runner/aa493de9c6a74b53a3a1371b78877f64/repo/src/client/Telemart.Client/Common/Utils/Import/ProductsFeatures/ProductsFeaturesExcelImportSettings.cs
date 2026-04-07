using System;
using System.Collections.Generic;

namespace Telemart.Client.Common.Utils.Import.ProductsFeatures
{
    public class ProductsFeaturesExcelImportSettings
    {
        public ProductsFeaturesExcelImportSettings(
            IReadOnlyList<Tuple<string, string>> columns,
            string[] productNames)
        {
            Columns = columns;
            ProductNames = productNames;
        }

        public IReadOnlyList<Tuple<string, string>> Columns { get; }

        public string[] ProductNames { get; }
    }
}
