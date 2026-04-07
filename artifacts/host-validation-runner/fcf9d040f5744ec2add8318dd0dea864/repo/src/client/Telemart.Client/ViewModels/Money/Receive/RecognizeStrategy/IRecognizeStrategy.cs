using System.Collections.Generic;
using Telemart.Client.Data.Requests.Features.Order.Actions;

namespace Telemart.Client.ViewModels.Money.Receive.RecognizeStrategy
{
    public interface IRecognizeStrategy
    {
        IEnumerable<string> Validate(string text, IReadOnlyCollection<ReceiveViewItem> existingItems);

        SearchOrderRequest GetOrderRequest(string text);

        ReceiveViewItem CreateViewItem(int number, string text);
    }
}
