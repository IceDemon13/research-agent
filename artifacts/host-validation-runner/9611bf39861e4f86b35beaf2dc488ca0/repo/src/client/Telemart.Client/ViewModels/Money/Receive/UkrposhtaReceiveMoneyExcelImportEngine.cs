using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Telemart.Client.Common.Utils.Import;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Money.Receive
{
    public sealed class UkrposhtaReceiveMoneyExcelImportEngine : ExcelImportEngineBase<ReceiveViewItem, object>
    {
        private readonly string[] columnHeaders;
        private readonly Regex validatePrice;

        public UkrposhtaReceiveMoneyExcelImportEngine()
        {
            columnHeaders = new[] { "ШК", "Сумма" };
            validatePrice = new Regex(@"^-?\d+([.,]\d{0,2})?$");
        }

        protected override ReceiveViewItem MapRow(object[] row, object settings, int rowNumber)
        {
            ReceiveViewItem viewItem = new ReceiveViewItem
            {
                TrackNumber = row[0].ToString(),
                ActualAmount = decimal.Parse(row[1].ToString().Replace(",", ".")),
                Number = rowNumber - HeaderRowCount
            };

            return viewItem;
        }

        protected override int GetHeaderRowCount(object settings)
        {
            return 1;
        }

        protected override IEnumerable<ValidationResultItem> ValidateHeader(object[][] headerRows, object settings)
        {
            if (headerRows.Length != 1 || !HeaderIsValid(headerRows[0]))
            {
                yield return new ValidationResultItem("Неверная структура файла. Файл должен содержать две колонки с заголовками \"ШК\" и \"Сумма\"", true);
            }

            bool HeaderIsValid(object[] header)
            {
                string[] headers = header.Select(x => x.ToString()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

                return headers.Length == columnHeaders.Length
                       && columnHeaders
                          .Select((x, i) => new { columnHeader = x, fileHeader = headers[i] })
                          .All(x => x.columnHeader.Equals(x.fileHeader, StringComparison.OrdinalIgnoreCase));
            }
        }

        protected override IEnumerable<ValidationResultItem> ValidateRowBeforeMap(object[] row, object settings, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(row[0].ToString()) || string.IsNullOrWhiteSpace(row[1].ToString()))
            {
                yield return new ValidationResultItem($"Строка {rowNumber}: ШК и сумма оплаты не должны быть пустыми", true);
            }
            else if (!decimal.TryParse(row[1].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal _))
            {
                yield return new ValidationResultItem($"Строка {rowNumber}: Некорректный формат данных", true);
            }
            else
            {
                if (!validatePrice.IsMatch(row[1].ToString()))
                {
                    yield return new ValidationResultItem($"Строка {rowNumber}: Некорректный формат данных", true);
                }
            }
        }

        protected override IEnumerable<ValidationResultItem> ValidateRowAfterMap(ReceiveViewItem row, object settings, int rowNumber)
        {
            if (row.OrderId < 0 || row.ActualAmount < 0)
            {
                yield return new ValidationResultItem($"Строка {rowNumber}: сумма оплаты не должна быть отрицательной", true);
            }
        }
    }
}