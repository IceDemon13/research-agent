using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Data.Extensions;
using Telemart.Client.Common.Utils.Import;
using Telemart.Client.TransferObjects.NovaposhtaBill;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Novaposhta.NovaposhtaBill
{
    public class NovaposhtaBillExcellImportEngine : ExcelImportEngineBase<NovaposhtaBillTtnDto, object>
    {
        private const string RouteColumn = "route";
        private const string SenderColumn = "sender";
        private const string RecipientColumn = "recipient";
        private const string SenderContactColumn = "sender_contact";
        private const string RecipientContactColumn = "recipient_contact";

        private readonly char[] routeSeparators = { '-' };
        private readonly string[] _tableHeaderSign = { "№ з/п", "Документ ЕН" };
        private readonly string[] _tableBottonSign = { "Разом" };
        private readonly Dictionary<string, int> columnPositions = new Dictionary<string, int>();

        private int _headerPosition = 0;
        private int _bottonPosition = 0;

        protected override NovaposhtaBillTtnDto MapRow(object[] row, object settings, int rowNumber)
        {
            if (rowNumber <= _headerPosition + 1
                || string.IsNullOrWhiteSpace(row[columnPositions[nameof(NovaposhtaBillTtnDto.Ttn)]].ToString())
                || rowNumber >= _bottonPosition + 1)
            {
                return null;
            }

            NovaposhtaBillTtnDto mappedRow = new NovaposhtaBillTtnDto
            {
                Ttn = row[columnPositions[nameof(NovaposhtaBillTtnDto.Ttn)]].ToString()!.Trim(),
                Description = row[columnPositions[nameof(NovaposhtaBillTtnDto.Description)]].ToString()!.Trim(),
                Comment = string.Empty
            };

            if (decimal.TryParse(row[columnPositions[nameof(NovaposhtaBillTtnDto.Amount)]].ToString(), out decimal amount))
            {
                mappedRow.Amount = amount;
            }

            if (DateTime.TryParse(row[columnPositions[nameof(NovaposhtaBillTtnDto.Date)]].ToString(), out DateTime date))
            {
                mappedRow.Date = date;
            }

            if (decimal.TryParse(row[columnPositions[nameof(NovaposhtaBillTtnDto.Price)]].ToString(), out decimal price))
            {
                mappedRow.Price = price;
            }

            if (double.TryParse(row[columnPositions[nameof(NovaposhtaBillTtnDto.WeightReal)]].ToString(), out double weightReal))
            {
                mappedRow.WeightReal = weightReal;
            }

            string route = row[columnPositions[RouteColumn]].ToString()!;

            string[] routeParts = route.Split(routeSeparators, StringSplitOptions.RemoveEmptyEntries);

            if (routeParts.Length == 2)
            {
                mappedRow.CitySender = routeParts[0].Trim();
                mappedRow.CityRecipient = routeParts[1].Trim();
            }
            else if (routeParts.Length > 2)
            {
                Queue<string> routeQueue = new Queue<string>(routeParts);
                string senderLoc = row[columnPositions[SenderColumn]].ToString();
                string recipientLock = row[columnPositions[RecipientColumn]].ToString();

                mappedRow.CitySender = GetCityName(routeQueue, senderLoc, false);
                mappedRow.CityRecipient = GetCityName(new Queue<string>(routeQueue.Reverse()), recipientLock, true);

                if (string.IsNullOrEmpty(mappedRow.CitySender) && !string.IsNullOrEmpty(mappedRow.CityRecipient))
                {
                    mappedRow.CitySender = route.Replace($"-{mappedRow.CityRecipient}", string.Empty);
                }
                else if (!string.IsNullOrEmpty(mappedRow.CitySender) && string.IsNullOrEmpty(mappedRow.CityRecipient))
                {
                    mappedRow.CityRecipient = route.Replace($"{mappedRow.CitySender}-", string.Empty);
                }
            }

            mappedRow.Weight = double.TryParse(row[columnPositions[nameof(NovaposhtaBillTtnDto.Weight)]].ToString(), out double weight)
                ? weight
                : mappedRow.WeightReal;

            return mappedRow;
        }

        protected override int GetHeaderRowCount(object settings)
        {
            return 0;
        }

        protected override IEnumerable<ValidationResultItem> ValidateFile(ExcelReadResult excelData, object settings)
        {
            _headerPosition = 0;
            _bottonPosition = 0;

            columnPositions.Clear();

            for (int i = 0; i < excelData.DataRows.Length; i++)
            {
                object[] columns = excelData.DataRows[i];

                if (_headerPosition == 0 && columns.Length > 0 && columns.Any(x => _tableHeaderSign.Contains(x.ToString())))
                {
                    _headerPosition = i;
                }

                if (_headerPosition > 0
                    && _bottonPosition == 0
                    && (columns.Any(x => _tableBottonSign.Contains(x.ToString())) || columns.All(x => string.IsNullOrEmpty(x.ToString()))))
                {
                    _bottonPosition = i;
                }
            }

            if (_headerPosition == 0)
            {
                yield return new ValidationResultItem("Не найдена шапка таблицы", true);
            }

            Dictionary<string, string> columnMap = new Dictionary<string, string>
            {
                [nameof(NovaposhtaBillTtnDto.Ttn)] = "Документ ЕН",
                [nameof(NovaposhtaBillTtnDto.Amount)] = "Вартість вантажу",
                [nameof(NovaposhtaBillTtnDto.Date)] = "Дата ЕН",
                [nameof(NovaposhtaBillTtnDto.Price)] = "Сума",
                [nameof(NovaposhtaBillTtnDto.Description)] = "Опис вантажу",
                [nameof(NovaposhtaBillTtnDto.Weight)] = "Вага",
                [nameof(NovaposhtaBillTtnDto.WeightReal)] = "Вага",
                [RouteColumn] = "Маршрут",
                [SenderColumn] = "Відправник",
                [RecipientColumn] = "Отримувач",
                [SenderContactColumn] = "Контакт відправника",
                [RecipientContactColumn] = "Контакт отримувача"
            };

            string[] header = excelData.DataRows[_headerPosition].Select(x => x.ToString()).ToArray();

            foreach (KeyValuePair<string, string> column in columnMap)
            {
                columnPositions[column.Key] = header.FindIndex(x => x == column.Value);
            }

            foreach (KeyValuePair<string, int> column in columnPositions.Where(x => x.Value < 0))
            {
                yield return new ValidationResultItem($"Столбец \"{columnMap[column.Key]}\" не найден", true);
            }
        }

        private static string GetCityName(Queue<string> parts, string checkString, bool endToStart)
        {
            string candidate = parts.Peek();
            string city = string.Empty;

            while (checkString.Contains(candidate) && parts.Count > 0)
            {
                parts.Dequeue();
                city = candidate;

                if (parts.Count > 0)
                {
                    candidate = endToStart
                        ? $"{parts.Peek()}-{candidate}"
                        : $"{candidate}-{parts.Peek()}";
                }
            }

            return city;
        }
    }
}