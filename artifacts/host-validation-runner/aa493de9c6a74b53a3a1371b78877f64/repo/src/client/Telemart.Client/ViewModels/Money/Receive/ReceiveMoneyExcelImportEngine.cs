using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Telemart.Client.Common.Utils.Import;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Money.Receive
{
    public sealed class ReceiveMoneyExcelImportEngine : ExcelImportEngineBase<ReceiveViewItem, object>
    {
        private readonly string[] columnHeaders;
        private readonly string[] columnHeadersWithCommission;
        private readonly Regex validatePrice;
        private bool isComission = false;

        public ReceiveMoneyExcelImportEngine()
        {
            columnHeaders = new[] { "Заказ", "Сумма" };
            columnHeadersWithCommission = new[] { "Заказ", "Сумма", "Комиссия" };
            validatePrice = new Regex(@"^-?\d+([.]\d{0,2})?$");
        }

        protected override ReceiveViewItem MapRow(object[] row, object settings, int rowNumber)
        {
            ReceiveViewItem viewItem = new ReceiveViewItem
            {
                OrderId = int.Parse(row[0].ToString()),
                ActualAmount = decimal.Parse(row[1].ToString()),
                CodCommission = isComission && decimal.TryParse(row[2].ToString(), out decimal codCommision) ? codCommision : null,
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
            isComission = false;

            if (headerRows.Length != 1
                || !HeaderIsValid(headerRows[0]))
            {
                yield return new ValidationResultItem("Неверная структура файла. Файл должен содержать две колонки с заголовками \"Заказ\" и \"Сумма\", а в случае необходимости внесения комиссии еще и колонку \"Комиссия\".", true);
            }

            bool HeaderIsValid(object[] header)
            {
                string[] headers = header.Select(x => x.ToString()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

                if (header.Length == 3)
                {
                    isComission = true;

                    return header.Length == columnHeadersWithCommission.Length
                           && columnHeadersWithCommission
                            .Select((x, i) => new { columnHeader = x, fileHeader = headers[i] })
                            .All(x => x.columnHeader.Equals(x.fileHeader, StringComparison.OrdinalIgnoreCase));
                }

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
                yield return new ValidationResultItem($"Строка {rowNumber}: Номер заказа и сумма оплаты не должны быть пустыми", true);
            }
            else if (!int.TryParse(row[0].ToString(), out int _) ||
                !decimal.TryParse(row[1].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal _))
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

            if (isComission
                && !string.IsNullOrEmpty(row[2].ToString())
                && !decimal.TryParse(row[2].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal _))
            {
                yield return new ValidationResultItem($"Строка {rowNumber}: Некорректный формат данных в поле Комиссия", true);
            }
        }

        protected override IEnumerable<ValidationResultItem> ValidateRowAfterMap(ReceiveViewItem row, object settings, int rowNumber)
        {
            if (row.OrderId < 0 || row.ActualAmount < 0)
            {
                yield return new ValidationResultItem($"Строка {rowNumber}: Номер заказа и сумма оплаты не должны быть отрицательными", true);
            }
        }
    }
}