using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Telemart.Client.Data.Requests.Features.Order.Actions;

namespace Telemart.Client.ViewModels.Money.Receive.RecognizeStrategy
{
    public sealed class TrackNumberRecognizeStrategy : IRecognizeStrategy
    {
        public IEnumerable<string> Validate(string text, IReadOnlyCollection<ReceiveViewItem> existingItems)
        {
            if (!Regex.IsMatch(text, @"^\d{11}$") && !Regex.IsMatch(text, @"^\d{14}$"))
            {
                yield return "Неверное количество символов";
                yield break;
            }

            if (existingItems.Any(x => string.Equals(x.TrackNumber, text, StringComparison.OrdinalIgnoreCase)))
            {
                yield return "Такой номер ТТН уже внесен";
            }
        }

        public SearchOrderRequest GetOrderRequest(string text)
        {
            return new SearchOrderRequest(text);
        }

        public ReceiveViewItem CreateViewItem(int number, string text)
        {
            return new ReceiveViewItem(number, text)
            {
                TrackNumber = text.Trim()
            };
        }
    }
}
