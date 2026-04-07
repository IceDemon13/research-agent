using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Telemart.Client.Data.Requests.Features.Order.Actions;

namespace Telemart.Client.ViewModels.Money.Receive.RecognizeStrategy
{
    public sealed class OrderRecognizeStrategy : IRecognizeStrategy
    {
        public IEnumerable<string> Validate(string text, IReadOnlyCollection<ReceiveViewItem> existingItems)
        {
            if (!Regex.IsMatch(text, @"^\d{6,7}$"))
            {
                yield return "Неверное количество символов";
                yield break;
            }

            if (!int.TryParse(text, out int orderId))
            {
                yield return "Неверный формат номера заказа";
                yield break;
            }

            if (existingItems.Any(x => x.OrderId == orderId))
            {
                yield return "Такой номер заказа уже внесен";
            }
        }

        public SearchOrderRequest GetOrderRequest(string text)
        {
            return new SearchOrderRequest(int.Parse(text));
        }

        public ReceiveViewItem CreateViewItem(int number, string text)
        {
            return new ReceiveViewItem(number, text)
            {
                OrderId = int.Parse(text)
            };
        }
    }
}
