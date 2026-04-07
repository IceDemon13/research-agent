using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Helpers;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.SupplierBill.Create.Parsing
{
    public static class SupplierBillTextParser
    {
        public static SupplierBillTextParserResult Parse(string text, SupplierBillTextParserSettings settings)
        {
            List<CreateSupplierBillProductModel> items = new List<CreateSupplierBillProductModel>();

            string[][] result = ParseText(text);

            List<ValidationResultItem> validationItems = new List<ValidationResultItem>();

            bool hasErrors = false;

            foreach (ValidationResultItem validationResultItem in ValidateContent(result, settings))
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
                            CreateSupplierBillProductModel item = MapRow(dataRow, settings);
                            items.Add(item);
                        }
                    }

                    i++;
                }
            }

            return hasErrors
                ? new SupplierBillTextParserResult(validationItems)
                : new SupplierBillTextParserResult(items);
        }

        private static string[][] ParseText(string text)
        {
            return text.Split(new[] { "\r\n" }, StringSplitOptions.None)
                .Select(x => !string.IsNullOrWhiteSpace(x) ? x.Split(new[] { "\t" }, StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>())
                .ToArray();
        }

        private static IEnumerable<ValidationResultItem> ValidateContent(string[][] data, SupplierBillTextParserSettings settings)
        {
            if (data.Length <= 0)
            {
                yield return new ValidationResultItem("Нет данных для обработки", true);
            }
        }

        private static IEnumerable<ValidationResultItem> ValidateRow(string[] row, SupplierBillTextParserSettings settings, int lineNumber)
        {
            int maxIndex = new[] { settings.CodeIndex, settings.NameIndex, settings.PriceIndex, settings.QuantityIndex, settings.TnvedIndex }.Max() + 1;

            if (row.Length != maxIndex)
            {
                string columns = WordEndingHelper.GetWordByNumber(maxIndex, "колонка", "колонки", "колонок");

                yield return new ValidationResultItem($"Строка {lineNumber}. Строка должна содержать {maxIndex} {columns}", true);
                yield break;
            }

            string q = GetValue(row, settings.QuantityIndex);
            string p = GetValue(row, settings.PriceIndex);
            string t = GetValue(row, settings.TnvedIndex);

            if (int.TryParse(q, out int quantity))
            {
                if (quantity <= 0)
                {
                    yield return new ValidationResultItem($"Строка {lineNumber}. Кол-во не может быть <= 0", true);
                }
            }
            else
            {
                yield return new ValidationResultItem($"Строка {lineNumber}. Невалидное значение в колонке количество", true);
            }

            if (decimal.TryParse(p, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.GetCultureInfo("ru-ru"), out decimal price)
                || decimal.TryParse(p, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.GetCultureInfo("en-us"), out price))
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

            if (t != null && (t.Any(x => !char.IsNumber(x)) || t.Length < 5 || t.Length > 10 || !long.TryParse(t, out long _)))
            {
                yield return new ValidationResultItem($"Строка {lineNumber}. Код УКТВЭД должен содержать только числовое значение длиной 5 - 10 символов", true);
            }
        }

        private static CreateSupplierBillProductModel MapRow(string[] row, SupplierBillTextParserSettings settings)
        {
            string p = GetValue(row, settings.PriceIndex);

            if (!decimal.TryParse(p, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.GetCultureInfo("ru-ru"), out decimal price))
            {
                decimal.TryParse(p, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.GetCultureInfo("en-us"), out price);
            }

            return new CreateSupplierBillProductModel(
                GetValue(row, settings.NameIndex),
                GetValue(row, settings.CodeIndex),
                int.Parse(GetValue(row, settings.QuantityIndex)),
                price,
                settings.PriceWithTax,
                long.TryParse(GetValue(row, settings.TnvedIndex), out long tnved) ? tnved : (long?)null);
        }

        private static string GetValue(string[] row, int index)
        {
            return index < 0
                ? null
                : row[index];
        }
    }
}