using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Telemart.Client.Common.Utils.Import;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Money.Receive
{
    public sealed class MeestReceiveMoneyExcelImportEngine : ExcelImportEngineBase<ReceiveViewItem, object>
    {
        private readonly string[] _columnHeaders = new[] { "ТТН", "Дата", "Сума" };
        private readonly Regex _validatePrice = new Regex(@"^-?\d+([.,]\d{0,2})?$");

        protected override ReceiveViewItem MapRow(object[] row, object settings, int rowNumber)
        {
            ReceiveViewItem viewItem = new ReceiveViewItem
            {
                TrackNumber = row[0].ToString(),
                Received = DateTime.TryParse(row[1].ToString(), out DateTime date) && date > DateTime.Now.AddMonths(-1) ? date : null,
                ActualAmount = decimal.Parse(row[2].ToString().Replace(",", ".")),
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
                yield return new ValidationResultItem("Неверная структура файла. Файл должен содержать три колонки с заголовками \"ТТН\", \"Дата\" и \"Сума\"", true);
            }

            bool HeaderIsValid(object[] header)
            {
                string[] headers = header.Select(x => x.ToString()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

                return headers.Length == _columnHeaders.Length
                       && _columnHeaders
                           .Select((x, i) => new { columnHeader = x, fileHeader = headers[i] })
                           .All(x => x.columnHeader.Equals(x.fileHeader, StringComparison.OrdinalIgnoreCase));
            }
        }

        protected override IEnumerable<ValidationResultItem> ValidateRowBeforeMap(object[] row, object settings, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(row[0].ToString()) || string.IsNullOrWhiteSpace(row[2].ToString()))
            {
                yield return new ValidationResultItem($"Строка {rowNumber}: ттн и сумма оплаты не должны быть пустыми", true);
                yield break;
            }

            if (!decimal.TryParse(row[2].ToString().Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal _))
            {
                yield return new ValidationResultItem($"Строка {rowNumber}: Некорректный формат данных", true);
            }
            else
            {
                if (!_validatePrice.IsMatch(row[2].ToString()))
                {
                    yield return new ValidationResultItem($"Строка {rowNumber}: Некорректный формат данных цены", true);
                }
            }
        }
    }
}