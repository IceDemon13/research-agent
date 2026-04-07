using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telemart.Client.Common;
using Telemart.Client.Common.Utils.Import;
using Telemart.Client.Data.Requests.Features.Products.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Catalog;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Directories.ProductsCatalog
{
    public class ProductsCatalogExcelImportEngine : ExcelImportEngineBase<ProductCatalogImportItem, ProductsCatalogExcelImportSettings>
    {
        private readonly IWebClient webClient;
        private Dictionary<string, int> columnMap;

        public ProductsCatalogExcelImportEngine(IWebClient webClient)
        {
            this.webClient = webClient;
        }

        protected override ProductCatalogImportItem MapRow(object[] row, ProductsCatalogExcelImportSettings settings, int rowNumber)
        {
            if (row == null)
            {
                return null;
            }

            string warrantyRetail = GetStringValue(nameof(ProductCatalogViewItem.WarrantyRetailId));
            string warrantyWholesale = GetStringValue(nameof(ProductCatalogViewItem.WarrantyWholesaleId));
            string warrantyType = GetStringValue(nameof(ProductCatalogViewItem.WarrantyTypeId));
            string productType = GetStringValue(nameof(ProductCatalogViewItem.TypeId));

            settings.AvailableWarranties.TryGetValue(warrantyRetail, out int warrantyRetailId);
            settings.AvailableWarranties.TryGetValue(warrantyWholesale, out int warrantyWholesaleId);
            settings.WarrantyTypes.TryGetValue(warrantyType, out int warrantyTypeId);
            settings.ProductTypes.TryGetValue(productType, out int productTypeId);

            ProductCatalogImportItem result = new ProductCatalogImportItem(
                GetIntValue(nameof(ProductCatalogViewItem.Id)),
                GetStringValue(nameof(ProductCatalogViewItem.YandexId)),
                GetStringValue(nameof(ProductCatalogViewItem.Color)),
                GetStringValue(nameof(ProductCatalogViewItem.ColorPrimary)),
                GetStringValue(nameof(ProductCatalogViewItem.ColorSecondary)),
                GetStringValue(nameof(ProductCatalogViewItem.Keywords)),
                GetStringValue(nameof(ProductCatalogViewItem.Manufactor)),
                GetStringValue(nameof(ProductCatalogViewItem.Model)),
                GetStringValue(nameof(ProductCatalogViewItem.ModelUkr)),
                GetStringValue(nameof(ProductCatalogViewItem.ModelEn)),
                GetStringValue(nameof(ProductCatalogViewItem.Modific)),
                GetStringValue(nameof(ProductCatalogViewItem.Name)),
                GetStringValue(nameof(ProductCatalogViewItem.NameUkr)),
                GetStringValue(nameof(ProductCatalogViewItem.NameEn)),
                GetStringValue(nameof(ProductCatalogViewItem.PartNumber)),
                GetStringValue(nameof(ProductCatalogViewItem.PrefixRus)),
                GetStringValue(nameof(ProductCatalogViewItem.PrefixUkr)),
                GetStringValue(nameof(ProductCatalogViewItem.PrefixEn)),
                GetIntValue(nameof(ProductCatalogViewItem.CategoryId)),
                warrantyRetailId,
                warrantyWholesaleId,
                warrantyTypeId,
                productTypeId,
                GetStringValue(nameof(ProductCatalogViewItem.GroupName)),
                GetIntValue(nameof(ProductCatalogViewItem.GroupFeatureId)));

            return result;

            int GetIntValue(string propertyName)
            {
                object value = row[columnMap[settings.AllowedColumns[propertyName]]];
                _ = int.TryParse(value.ToString(), out int intValue);
                return intValue;
            }

            string GetStringValue(string propertyName)
            {
                object rawValue = row[columnMap[settings.AllowedColumns[propertyName]]];

                string stringValue = rawValue.ToString();

                return Regex.Replace(stringValue, @"\s+", " ").Trim();
            }
        }

        protected override int GetHeaderRowCount(ProductsCatalogExcelImportSettings settings)
        {
            return 1;
        }

        protected override IEnumerable<ValidationResultItem> ValidateHeader(object[][] headerRows, ProductsCatalogExcelImportSettings settings)
        {
            columnMap = headerRows[0].Select((x, i) => new { x, i }).ToDictionary(x => x.x.ToString(), y => y.i);

            string[] notFoundColumns = settings.AllowedColumns
                .Where(x => !columnMap.ContainsKey(x.Value))
                .Select(x => $"\"{x.Value}\"")
                .ToArray();

            if (notFoundColumns.Any())
            {
                yield return new ValidationResultItem($"Нет соответствующих колонок в файле: {string.Join(", ", notFoundColumns)}", true);
            }
        }

        protected override IEnumerable<ValidationResultItem> ValidateFile(ExcelReadResult excelData, ProductsCatalogExcelImportSettings settings)
        {
            if (excelData.DataRows.Length > settings.MaxRowsForImport)
            {
                yield return new ValidationResultItem($"Файл должен содержать не более {settings.MaxRowsForImport} строк", true);
            }
        }

        protected override IEnumerable<ValidationResultItem> ValidateRows(ProductsCatalogExcelImportSettings settings)
        {
            int[] duplicateIds = Rows.Where(x => x.Id != 0).GroupBy(x => x.Id).Where(x => x.Count() > 1).Select(x => x.Key).ToArray();
            string[] duplicateNames = Rows.GroupBy(x => x.Name).Where(x => x.Count() > 1).Select(x => $"\"{x.Key}\"").ToArray();
            string[] duplicateNamesUkr = Rows.GroupBy(x => x.NameUkr).Where(x => x.Count() > 1).Select(x => $"\"{x.Key}\"").ToArray();
            string[] duplicateNamesEn = Rows.GroupBy(x => x.NameEn).Where(x => x.Count() > 1).Select(x => $"\"{x.Key}\"").ToArray();
            HashSet<int> notValidCategoryIds = Rows.Select(x => x.CategoryId).Where(x => !settings.AvailableCategories.Contains(x)).ToHashSet();
            HashSet<string> notValidColors = Rows
                .SelectMany(GetColors)
                .Where(x => !string.IsNullOrEmpty(x)
                    && !settings.AvailableColors.Contains(x, StringComparer.OrdinalIgnoreCase))
                .Select(x => $"\"{x}\"")
                .ToHashSet();

            if (duplicateIds.Any())
            {
                yield return new ValidationResultItem($"В файле содержатся дубликаты Id: {string.Join(", ", duplicateIds)}", true);
            }

            if (duplicateNames.Any())
            {
                yield return new ValidationResultItem($"В файле содержатся дубликаты названий товаров: {string.Join(", ", duplicateNames)}", true);
            }

            if (duplicateNamesUkr.Any())
            {
                yield return new ValidationResultItem($"В файле содержатся дубликаты украинских названий товаров: {string.Join(", ", duplicateNamesUkr)}", true);
            }

            if (duplicateNamesEn.Any())
            {
                yield return new ValidationResultItem($"В файле содержатся дубликаты украинских названий товаров: {string.Join(", ", duplicateNamesUkr)}", true);
            }

            if (notValidCategoryIds.Any())
            {
                yield return new ValidationResultItem($"В файле содержатся несуществующие Id категорий: {string.Join(", ", notValidCategoryIds)}", true);
            }

            if (notValidColors.Any())
            {
                yield return new ValidationResultItem($"В файле содержатся цвета, которых нет в бд: {string.Join(", ", notValidColors)}", true);
            }
        }

        protected override async Task<IReadOnlyCollection<ValidationResultItem>> PostProcessAsync(ProductsCatalogExcelImportSettings settings)
        {
            ProductCatalogSearchRequest searchRequest = new ProductCatalogSearchRequest
            {
                ProductIds = Rows.Select(x => x.Id).ToArray(),
                ProductNames = Rows.Select(x => x.Name).ToArray()
            };

            ProductsCatalogSearchResponse products = await webClient.ExecuteApiRequestAsync(new SearchProductsCatalog(searchRequest));
            Dictionary<string, ProductCatalogDto> serverProductsByNameDictionary = products.Products.ToDictionary(x => x.Name.ToLower());
            Dictionary<int, ProductCatalogDto> serverProductsByIdDictionary = products.Products.ToDictionary(x => x.Id);

            List<ValidationResultItem> allErrors = new List<ValidationResultItem>();

            foreach (ProductCatalogImportItem row in Rows)
            {
                ProductCatalogDto serverProduct = FindServerProduct(row, serverProductsByNameDictionary, serverProductsByIdDictionary);
                row.Dto = serverProduct;

                allErrors.AddRange(PostProcessValidateRow(row, serverProduct));
            }

            return allErrors;
        }

        private IEnumerable<ValidationResultItem> PostProcessValidateRow(ProductCatalogImportItem row, ProductCatalogDto serverProduct)
        {
            if (serverProduct == null)
            {
                if (row.Id != 0)
                {
                    yield return new ValidationResultItem($"Продукт с Id={row.Id} не существует", true);
                }
            }
            else if (serverProduct.Id != row.Id)
            {
                yield return new ValidationResultItem($"\"{row.Name}\" уже существует с кодом {serverProduct.Id}. Код в Excel: {row.Id}", true);
            }
        }

        private ProductCatalogDto FindServerProduct(IUniqueItem item, Dictionary<string, ProductCatalogDto> serverProductsByNameDictionary, Dictionary<int, ProductCatalogDto> serverProductsByIdDictionary)
        {
            string itemName = item.Name?.ToLower() ?? string.Empty;
            serverProductsByNameDictionary.TryGetValue(itemName, out ProductCatalogDto serverProduct);
            if (serverProduct == null)
            {
                serverProductsByIdDictionary.TryGetValue(item.Id, out serverProduct);
            }

            return serverProduct;
        }

        private IEnumerable<string> GetColors(ProductCatalogImportItem item)
        {
            yield return item.ColorPrimary;
            yield return item.ColorSecondary;
        }
    }
}