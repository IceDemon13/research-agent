using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DevExpress.Mvvm.Native;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.Common.Utils.Import.ProductsFeatures
{
    public class ProductsFeaturesExcelImportEngine : ExcelImportEngineBase<IReadOnlyDictionary<string, object>, ProductsFeaturesExcelImportSettings>
    {
        private const string NameFieldName = "Name";

        private List<string> fileColumnHeaders;

        protected override IReadOnlyDictionary<string, object> MapRow(object[] row, ProductsFeaturesExcelImportSettings settings, int rowNumber)
        {
            Dictionary<string, object> mappedRow = new Dictionary<string, object>();

            foreach (Tuple<string, string> originalColumn in settings.Columns)
            {
                int fileColumnIndex = fileColumnHeaders.FindIndex(x => x == originalColumn.Item2);

                if (fileColumnIndex == -1)
                {
                    continue;
                }

                if (originalColumn.Item1 == NameFieldName && string.IsNullOrWhiteSpace(row[fileColumnIndex]?.ToString()))
                {
                    return null;
                }

                mappedRow.Add(originalColumn.Item1, row[fileColumnIndex]);
            }

            return mappedRow;
        }

        protected override int GetHeaderRowCount(ProductsFeaturesExcelImportSettings settings)
        {
            return 2;
        }

        protected override IEnumerable<ValidationResultItem> ValidateHeader(object[][] headerRows, ProductsFeaturesExcelImportSettings settings)
        {
            if (headerRows.Length != 2)
            {
                yield return new ValidationResultItem("В файле должно быть 2 строки заголовка", true);
                yield break;
            }

            string groupHeader = string.Empty;

            fileColumnHeaders = new List<string>();

            for (int i = 0; i < headerRows[1].Length; i++)
            {
                string sourceGroupHeader = headerRows[0][i].ToString();
                if (!string.IsNullOrWhiteSpace(sourceGroupHeader))
                {
                    groupHeader = sourceGroupHeader;
                }

                string columnHeader = headerRows[1][i].ToString();

                string fullColumnHeader = $"{groupHeader}.{columnHeader}";

                fileColumnHeaders.Add(fullColumnHeader);

                if (settings.Columns.All(x => x.Item2 != fullColumnHeader))
                {
                    yield return new ValidationResultItem($"Характеристика \"{fullColumnHeader}\" не распознана", true);
                }
            }

            foreach (IGrouping<string, string> grouping in fileColumnHeaders.GroupBy(x => x).Where(x => x.Count() > 1))
            {
                yield return new ValidationResultItem($"В файле найдена дублирующаяся характеристика \"{grouping.Key}\"", true);
            }
        }

        protected override IEnumerable<ValidationResultItem> ValidateRows(ProductsFeaturesExcelImportSettings settings)
        {
            foreach (IReadOnlyDictionary<string, object> product in Rows.Where(x => !settings.ProductNames.Contains(x.GetValueOrDefault(NameFieldName))))
            {
                string productName = product.GetValueOrDefault(NameFieldName, string.Empty).ToString();

                yield return new ValidationResultItem($"Товар {productName} не распознан", true);
            }
        }
    }
}