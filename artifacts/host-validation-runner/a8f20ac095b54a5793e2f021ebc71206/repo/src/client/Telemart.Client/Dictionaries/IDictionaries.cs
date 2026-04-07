using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Dictionaries
{
    public interface IDictionaries
    {
        T GetItemById<T>(int id)
            where T : DictionaryItemBase;

        T GetItemByName<T>(string name)
            where T : DictionaryItemBase;

        IReadOnlyCollection<T> GetItems<T>()
            where T : DictionaryItemBase;

        IEnumerable<Currency> GetCurrencies();

        OrderProductSource GetOrderProductSource(int sourceId, int? warehouseId, string sourceText, DateTime? sourceDate);

        IEnumerable<int> GetPaymentsBySubdivision(int subdivisionId);

        IEnumerable<Payment> GetRefundPayments(int[] paymentIds, int? selectedPaymentId);

        IEnumerable<Payment> GetRefundPayments(params int[] paymentIds);

        IEnumerable<Payment> GetServiceRequestPaymentTypes(int orderPaymentId, IReadOnlyCollection<OrderPaymentDto> orderPayments, int orderContractorLimit = 0);

        Task LoadAsync();
    }
}