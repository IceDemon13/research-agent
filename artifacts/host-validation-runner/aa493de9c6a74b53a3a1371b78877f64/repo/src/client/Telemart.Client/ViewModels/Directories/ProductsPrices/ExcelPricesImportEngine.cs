using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Text;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.CodeView;
using OfficeOpenXml;
using Telemart.Client.Common.Utils.Import;
using Telemart.Client.Core.Extensions;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.ParserSettings;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Directories.ProductsPrices
{
    public class ExcelPricesImportEngine : ExcelImportEngineBase<ProductSaveDto, ParserSettingsDto>
    {
        private IReadOnlyDictionary<string, ParserSettingsAvailabilityDto> availsFromSettings;
        private IReadOnlyDictionary<string, ParserSettingsCategoryDto> categoriesFromSettings;
        private string currentCategory;
        private bool categoryGroupsMode;
        private bool allFileAvail;
        private int[] categoryColumnNumbers;
        private int[] nameColumnNumbers;
        private int[] codeColumnNumbers;
        private int[] pnColumnNumbers;

        protected override ProductSaveDto MapRow(object[] row, ParserSettingsDto settings, int rowNumber)
        {
            (int parserCategoryId, bool rowOnlyForCategory) categoryResult = CalculateParserCategoryId(row, settings);

            if (categoryResult.rowOnlyForCategory)
            {
                return null;
            }

            List<ParserPriceDto> prices = new List<ParserPriceDto>(settings.ParserSettingsFile.FileColumn.Prices.Count);

            foreach (ParserSettingsFilePriceDto priceSetting in settings.ParserSettingsFile.FileColumn.Prices)
            {
                double price = 0;

                string rawPriceString = row.ElementAtOrDefault(priceSetting.ColumnNumber - 1)?.ToString()?.Trim();

                if (!string.IsNullOrWhiteSpace(rawPriceString))
                {
                    rawPriceString.TryParseDouble(out price);
                }

                prices.Add(new ParserPriceDto()
                {
                    Value = price,
                    Currency = priceSetting.CurrencyId,
                    PriceType = priceSetting.ParserPriceTypeId
                });
            }

            string avail = row.ElementAtOrDefault(settings.ParserSettingsFile.FileColumn.AvailColumnNumber!.Value - 1)?.ToString();

            SupplierWarehouseAvailDto[] avails = settings.ParserSettingsFile.SupplierWarehouseId.HasValue
                ? new[]
                {
                    new SupplierWarehouseAvailDto
                    {
                        SupplierWarehouseId = settings.ParserSettingsFile.SupplierWarehouseId.Value,
                        PriceAvail = avail
                    }
                }
                : Array.Empty<SupplierWarehouseAvailDto>();

            ProductSaveDto productSaveDto = new ProductSaveDto()
            {
                Name = GetConcatedName(row),
                Code = GetConcatedCode(row),
                Pn = GetContactedPartNumber(row),
                Prices = prices,
                SupplierWarehouseAvails = avails,
                AvailType = allFileAvail
                    ? ProductAvailabilityType.InStock.Id
                    : GetAvailTypeId(avail, availsFromSettings),
                ParserCategoryId = categoryResult.parserCategoryId
            };

            return productSaveDto;
        }

        protected override void Init(ParserSettingsDto settings)
        {
            availsFromSettings = settings.Availabilities.GroupBy(x => x.Avail).Select(x => x.First()).ToDictionary(x => x.Avail);

            categoriesFromSettings = settings.Categories.GroupBy(x => x.Url).Select(x => x.First()).ToDictionary(x => x.Url);

            categoryColumnNumbers = settings.ParserSettingsFile.FileColumn.CategoryColumnNumbers
                .Split(',')
                .Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(int.Parse)
                .ToArray();

            codeColumnNumbers = settings.ParserSettingsFile.FileColumn.CodeColumnNumbers?
                .Split(',')
                .Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(int.Parse)
                .ToArray();

            pnColumnNumbers = settings.ParserSettingsFile.FileColumn.PartNumberColumnNumbers?
                .Split(',')
                .Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(int.Parse)
                .ToArray();

            nameColumnNumbers = settings.ParserSettingsFile.FileColumn.NameColumnNumbers
                .Split(',')
                .Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(int.Parse)
                .ToArray();

            categoryGroupsMode = settings.ParserSettingsFile.FileColumn.CategoryRule1.Active
                                 || settings.ParserSettingsFile.FileColumn.CategoryRule2.Active
                                 || settings.ParserSettingsFile.FileColumn.CategoryRule3.Active;

            if (codeColumnNumbers?.Any() != true && pnColumnNumbers?.Any() != true)
            {
                throw new Exception($"Настройка столбца Код или Артикул должна быть установлена");
            }

            if (settings.Availabilities?.Any() != true && settings.ParserSettingsFile.FileColumn.AvailColumnNumber is null)
            {
                allFileAvail = true;
            }
        }

        protected override int GetHeaderRowCount(ParserSettingsDto settings)
        {
            return settings.ParserSettingsFile.StartLine - 1;
        }

        protected override IEnumerable<ValidationResultItem> ValidateRowAfterMap(ProductSaveDto row, ParserSettingsDto settings, int rowNumber)
        {
            if (row.AvailType == 0)
            {
                yield return new ValidationResultItem($"Строка {rowNumber}: не задано наличие", false);
            }

            if (string.IsNullOrWhiteSpace(row.Name) && string.IsNullOrWhiteSpace(row.Code))
            {
                yield return new ValidationResultItem($"Строка {rowNumber}: не заданы идентификаторы товара. (название или код)", false);
            }

            if (row.ParserCategoryId <= 0)
            {
                yield return new ValidationResultItem($"Строка {rowNumber}: не получилось определить категорию товара", false);
            }
        }

        protected override IEnumerable<ValidationResultItem> ValidateFile(ExcelReadResult excelData, ParserSettingsDto settings)
        {
            if (excelData.DataRows?.Any() != true)
            {
                yield return new ValidationResultItem("Пустой файл", true);
            }
        }

        protected override IEnumerable<ExcelWorksheet> GetWorksheetsToProcess(ExcelPackage package, ParserSettingsDto settings)
        {
            var a = base.GetWorksheetsToProcess(package, settings);

            return settings.ParserSettingsFile.SheetsTypeId switch
            {
                ParserSettingsFileSheetsType.OneId => [package.Workbook.Worksheets.FirstOrDefault()],
                ParserSettingsFileSheetsType.AllId => package.Workbook.Worksheets,
                ParserSettingsFileSheetsType.SelectivelyId =>
                    package.Workbook.Worksheets
                        .Where(x => settings.ParserSettingsFile.SheetsRecognizePattern.SplitCommas()
                            .Contains(settings.ParserSettingsFile.SheetsRecognizeTypeId switch
                            {
                                ParserSettingsFileSheetsRecognizeType.ByNameId => x.Name,
                                ParserSettingsFileSheetsRecognizeType.ByNumberId => (x.Index + 1).ToString(),
                                _ => string.Empty
                            })
                        ),
                _ => []
            };
        }

        private static int GetAvailTypeId(string avail, IReadOnlyDictionary<string, ParserSettingsAvailabilityDto> availsFromSettings)
        {
            ParserSettingsAvailabilityDto availabilityDto = availsFromSettings.GetValueOrDefault(avail);

            return availabilityDto?.AvailabilityTypeId ?? ProductAvailabilityType.NotInStock.Id;
        }

        private string GetConcatedName(IReadOnlyList<object> row)
        {
            return string.Join(' ', nameColumnNumbers.Select(x => row.ElementAtOrDefault(x - 1))).Trim();
        }

        private string GetConcatedCode(IReadOnlyList<object> row)
        {
            return codeColumnNumbers?.Any() == true
                ? string.Join(' ', codeColumnNumbers.Select(x => row.ElementAtOrDefault(x - 1))).Trim()
                : null;
        }

        private string GetContactedPartNumber(IReadOnlyList<object> row)
        {
            return pnColumnNumbers?.Any() == true
                ? string.Join(' ', pnColumnNumbers.Select(x => row.ElementAtOrDefault(x - 1))).Trim()
                : null;
        }

        private (int parserCategoryId, bool rowOnlyForCategory) CalculateParserCategoryId(IReadOnlyList<object> row, ParserSettingsDto settings)
        {
            string rowCategoryName = string.Join(' ', categoryColumnNumbers.Select(x => row.ElementAtOrDefault(x - 1))).Trim();

            bool rowOnlyForCategory = false;

            if (categoryGroupsMode)
            {
                bool categoryRule1ConditionSuccess;
                if (!settings.ParserSettingsFile.FileColumn.CategoryRule1.Active)
                {
                    categoryRule1ConditionSuccess = true;
                }
                else
                {
                    string cellValue = row.ElementAtOrDefault(settings.ParserSettingsFile.FileColumn.CategoryRule1.ColumnNumber - 1)?.ToString();

                    bool cellsEquals = string.Equals(
                                           cellValue,
                                           settings.ParserSettingsFile.FileColumn.CategoryRule1.Pattern,
                                           StringComparison.OrdinalIgnoreCase)
                                       || (string.IsNullOrWhiteSpace(cellValue) && settings.ParserSettingsFile.FileColumn.CategoryRule1.Pattern is null)
                                       || (string.IsNullOrWhiteSpace(settings.ParserSettingsFile.FileColumn.CategoryRule1.Pattern) && cellValue is null);

                    categoryRule1ConditionSuccess = settings.ParserSettingsFile.FileColumn.CategoryRule1.EqualsCondition ? cellsEquals : !cellsEquals;
                }

                bool categoryRule2ConditionSuccess;
                if (!settings.ParserSettingsFile.FileColumn.CategoryRule2.Active)
                {
                    categoryRule2ConditionSuccess = true;
                }
                else
                {
                    string cellValue = row.ElementAtOrDefault(settings.ParserSettingsFile.FileColumn.CategoryRule2.ColumnNumber - 1)?.ToString();

                    bool cellsEquals = string.Equals(
                        cellValue,
                        settings.ParserSettingsFile.FileColumn.CategoryRule2.Pattern,
                        StringComparison.OrdinalIgnoreCase)
                                       || (string.IsNullOrWhiteSpace(cellValue) && settings.ParserSettingsFile.FileColumn.CategoryRule2.Pattern is null)
                                       || (string.IsNullOrWhiteSpace(settings.ParserSettingsFile.FileColumn.CategoryRule2.Pattern) && cellValue is null);

                    categoryRule2ConditionSuccess = settings.ParserSettingsFile.FileColumn.CategoryRule2.EqualsCondition ? cellsEquals : !cellsEquals;
                }

                bool categoryRule3ConditionSuccess;
                if (!settings.ParserSettingsFile.FileColumn.CategoryRule3.Active)
                {
                    categoryRule3ConditionSuccess = true;
                }
                else
                {
                    string cellValue = row.ElementAtOrDefault(settings.ParserSettingsFile.FileColumn.CategoryRule3.ColumnNumber - 1)?.ToString();

                    bool cellsEquals = string.Equals(
                        cellValue,
                        settings.ParserSettingsFile.FileColumn.CategoryRule3.Pattern,
                        StringComparison.OrdinalIgnoreCase)
                                       || (string.IsNullOrWhiteSpace(cellValue) && settings.ParserSettingsFile.FileColumn.CategoryRule3.Pattern is null)
                                       || (string.IsNullOrWhiteSpace(settings.ParserSettingsFile.FileColumn.CategoryRule3.Pattern) && cellValue is null);

                    categoryRule3ConditionSuccess = settings.ParserSettingsFile.FileColumn.CategoryRule3.EqualsCondition ? cellsEquals : !cellsEquals;
                }

                if (categoryRule1ConditionSuccess && categoryRule2ConditionSuccess && categoryRule3ConditionSuccess)
                {
                    currentCategory = rowCategoryName;
                    rowOnlyForCategory = true;
                }
                else
                {
                    rowCategoryName = currentCategory;
                }
            }

            return (GetCategoryIdByUrl(rowCategoryName), rowOnlyForCategory);
        }

        private int GetCategoryIdByUrl(string categoryName)
        {
            ParserSettingsCategoryDto[] parserSettingsCategories = categoriesFromSettings.Values.ToArray();

            return parserSettingsCategories.FirstOrDefault(x =>
            {
                switch (x.UrlComparisonType)
                {
                    case ParserCategoryComparisonType.OrdinalId:
                        return string.Equals(x.Url, categoryName, StringComparison.OrdinalIgnoreCase);
                    case ParserCategoryComparisonType.LikeId:
                        return categoryName.Contains(x.Url, StringComparison.OrdinalIgnoreCase);
                    case ParserCategoryComparisonType.RegexId:
                        return System.Text.RegularExpressions.Regex.IsMatch(categoryName, x.Url, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    default:
                        return false;
                }
            })?.Id ?? 0;
        }
    }
}