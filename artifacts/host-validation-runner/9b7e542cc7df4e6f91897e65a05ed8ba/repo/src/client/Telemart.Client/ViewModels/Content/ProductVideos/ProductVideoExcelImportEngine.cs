using System;
using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Common.Utils.Import;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Content.ProductVideos
{
    public class ProductVideoExcelImportEngine : ExcelImportEngineBase<ProductVideoViewItem, object>
    {
        private const int HashLength = 11;

        protected override ProductVideoViewItem MapRow(object[] row, object settings, int rowNumber)
        {
            ProductVideoViewItem mappedRow = null;

            if (row.Length == 2 && !string.IsNullOrWhiteSpace(row[0].ToString()))
            {
                string productName = row[0].ToString().Trim();
                string hash = row[1].ToString().Trim();

                ProductVideoViewItem item = Rows.FirstOrDefault(x => x.ExcelProductName.Equals(productName, StringComparison.Ordinal));

                if (item == null)
                {
                    mappedRow = new ProductVideoViewItem
                    {
                        ExcelProductName = productName,
                        Hashes = new HashSet<string> { hash }
                    };
                }
                else
                {
                    item.Hashes.Add(hash);
                }
            }

            return mappedRow;
        }

        protected override int GetHeaderRowCount(object settings)
        {
            return 0;
        }

        protected override IEnumerable<ValidationResultItem> ValidateFile(ExcelReadResult excelData, object settings)
        {
            if (excelData.DataRows.Any(x => x.Length < 2))
            {
                yield return new ValidationResultItem("Файл должен содержать 2 колонки", true);
            }

            if (excelData.DataRows.All(x => string.IsNullOrWhiteSpace(x[0].ToString())))
            {
                yield return new ValidationResultItem("В первой колонке листа ожидается название товара", true);
            }
        }

        protected override IEnumerable<ValidationResultItem> ValidateRowAfterMap(ProductVideoViewItem row, object settings, int rowNumber)
        {
            if (row.Hashes.Any(x => x.Length != HashLength))
            {
                yield return new ValidationResultItem($"Длина хеш-кода для товара \"{row.ExcelProductName}\" должна быть {HashLength} символов", true);
            }
        }
    }
}