using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Telemart.Client.Core.Helpers;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Store.Invoice.Parsing
{
    public class InvoiceProductTextParser
    {
        public static InvoiceProductTextParserResult Parse(string text, InvoiceProductTextParserSettings settings)
        {
            List<InvoiceProductBulkAddViewItem> items = new();

            string[][] result = ParseText(text);

            List<ValidationResultItem> validationItems = new();

            bool hasErrors = false;

            foreach (ValidationResultItem validationResultItem in ValidateContent(result))
            {
                validationItems.Add(validationResultItem);

                if (validationResultItem.IsError)
                {
                    hasErrors = true;
                }
            }

            if (!hasErrors)
            {
                int i = 1;

                foreach (string[] dataRow in result)
                {
                    if (dataRow.Length > 0)
                    {
                        bool rowHasError = false;

                        foreach (ValidationResultItem validationResultItem in ValidateRow(dataRow, settings, i))
                        {
                            validationItems.Add(validationResultItem);

                            if (validationResultItem.IsError)
                            {
                                rowHasError = true;
                                hasErrors = true;
                            }
                        }

                        if (!rowHasError)
                        {
                            InvoiceProductBulkAddViewItem item = MapRow(dataRow, settings);
                            items.Add(item);
                        }
                    }

                    i++;
                }
            }

            return hasErrors
                ? new InvoiceProductTextParserResult(validationItems)
                : new InvoiceProductTextParserResult(items);
        }

        private static string[][] ParseText(string text)
        {
            return text.Split(new[] { "\r\n" }, StringSplitOptions.None)
                .Select(x => !string.IsNullOrWhiteSpace(x) ? x.Split(new[] { "\t" }, StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>())
                .ToArray();
        }

        private static IEnumerable<ValidationResultItem> ValidateRow(string[] row, InvoiceProductTextParserSettings settings, int lineNumber)
        {
            int maxIndex = new[]
            {
                settings.PnIndex,
                settings.PriceIndex,
                settings.QuantityIndex,
                settings.ProductIdIndex,
                settings.ProductNameIndex,
                settings.SupplierProductIdIndex,
                settings.SupplierProductNameIndex
            }.Max() + 1;

            if (row.Length != maxIndex)
            {
                string columns = WordEndingHelper.GetWordByNumber(maxIndex, "колонка", "колонки", "колонок");

                yield return new ValidationResultItem($"Строка {lineNumber}. Строка должна содержать {maxIndex} {columns}", true);
                yield break;
            }

            string quantityStr = GetValue(row, settings.QuantityIndex);
            string priceStr = GetValue(row, settings.PriceIndex);
            string productIdStr = GetValue(row, settings.ProductIdIndex);
            string productNameStr = GetValue(row, settings.ProductNameIndex);
            string pnStr = GetValue(row, settings.PnIndex);
            string supplierProductIdStr = GetValue(row, settings.SupplierProductIdIndex);
            string supplierProductNameStr = GetValue(row, settings.SupplierProductNameIndex);

            if (pnStr != null && pnStr.Length < 2)
            {
                yield return new ValidationResultItem($"Строка {lineNumber}. Артикул слишком короткий", true);
            }

            if (productNameStr != null && productNameStr.Length < 5)
            {
                yield return new ValidationResultItem($"Строка {lineNumber}. Название товара слишком короткое", true);
            }

            if (supplierProductIdStr != null && supplierProductIdStr.Length < 2)
            {
                yield return new ValidationResultItem($"Строка {lineNumber}. Код товара поставщика слишком короткий", true);
            }

            if (supplierProductNameStr != null && supplierProductNameStr.Length < 2)
            {
                yield return new ValidationResultItem($"Строка {lineNumber}. Название товара поставщика слишком короткое", true);
            }

            if (productIdStr != null)
            {
                if (int.TryParse(productIdStr, out int productId))
                {
                    if (productId <= 0)
                    {
                        yield return new ValidationResultItem($"Строка {lineNumber}. Код товара не может быть <= 0", true);
                    }
                }
                else
                {
                    yield return new ValidationResultItem($"Строка {lineNumber}. Невалидное значение в колонке \"Код товара\"", true);
                }
            }

            if (int.TryParse(quantityStr, out int quantity))
            {
                if (quantity <= 0)
                {
                    yield return new ValidationResultItem($"Строка {lineNumber}. Кол-во не может быть <= 0", true);
                }
            }
            else
            {
                yield return new ValidationResultItem($"Строка {lineNumber}. Невалидное значение в колонке \"Количество\"", true);
            }

            if (priceStr != null)
            {
                priceStr = ProcessPriceStr(priceStr);

                if (decimal.TryParse(priceStr, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.GetCultureInfo("ru-ru"), out decimal price)
                    || decimal.TryParse(priceStr, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.GetCultureInfo("en-us"), out price))
                {
                    if (price <= 0)
                    {
                        yield return new ValidationResultItem($"Строка {lineNumber}. Цена не может быть <= 0", true);
                    }
                    else
                    {
                        decimal val = price * 100;

                        if (val - (long)val > 0)
                        {
                            yield return new ValidationResultItem($"Строка {lineNumber}. Цена не может содержать больше 2-х разрядов после запятой", true);
                        }
                    }
                }
                else
                {
                    yield return new ValidationResultItem($"Строка {lineNumber}. Невалидное значение в колонке цена", true);
                }
            }
        }

        private static IEnumerable<ValidationResultItem> ValidateContent(IReadOnlyCollection<string[]> data)
        {
            if (data.Count <= 0)
            {
                yield return new ValidationResultItem("Нет данных для обработки", true);
            }
        }

        private static InvoiceProductBulkAddViewItem MapRow(IReadOnlyList<string> row, InvoiceProductTextParserSettings settings)
        {
            decimal parsedPrice = 0;

            string priceStr = GetValue(row, settings.PriceIndex);

            if (priceStr is not null)
            {
                priceStr = ProcessPriceStr(priceStr);

                if (!decimal.TryParse(priceStr, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.GetCultureInfo("ru-ru"), out parsedPrice))
                {
                    decimal.TryParse(priceStr, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.GetCultureInfo("en-us"), out parsedPrice);
                }
            }

            string productIdStr = GetValue(row, settings.ProductIdIndex);

            return new InvoiceProductBulkAddViewItem(
                productIdStr is null ? null : int.Parse(productIdStr),
                GetValue(row, settings.ProductNameIndex),
                GetValue(row, settings.SupplierProductIdIndex),
                GetValue(row, settings.SupplierProductNameIndex),
                GetValue(row, settings.PnIndex),
                int.Parse(GetValue(row, settings.QuantityIndex)),
                parsedPrice);
        }

        private static string GetValue(IReadOnlyList<string> row, int index)
        {
            return index < 0
                ? null
                : row[index];
        }

        private static string ProcessPriceStr(string priceStr)
        {
            priceStr = priceStr.Replace(" ", string.Empty);
            priceStr = priceStr.Replace(',', '.');
            priceStr = priceStr.Replace("_", string.Empty);
            priceStr = priceStr.Replace("$", string.Empty);

            return priceStr;
        }
    }
}