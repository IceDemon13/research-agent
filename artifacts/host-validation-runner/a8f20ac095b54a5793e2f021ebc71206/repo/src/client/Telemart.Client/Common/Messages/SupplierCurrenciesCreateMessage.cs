using System.Collections.ObjectModel;
using Telemart.Client.TransferObjects.SupplierCurrency;

namespace Telemart.Client.Common.Messages
{
    public sealed class SupplierCurrenciesCreateMessage
    {
        public SupplierCurrenciesCreateMessage(ReadOnlyObservableCollection<SupplierCurrencyRateHistoryDto> dtos, MessageType messageType)
        {
            SupplierCurrencyRateHistoryDtos = dtos;
            MessageType = messageType;
        }

        public ReadOnlyObservableCollection<SupplierCurrencyRateHistoryDto> SupplierCurrencyRateHistoryDtos { get; }

        public MessageType MessageType { get; }
    }
}