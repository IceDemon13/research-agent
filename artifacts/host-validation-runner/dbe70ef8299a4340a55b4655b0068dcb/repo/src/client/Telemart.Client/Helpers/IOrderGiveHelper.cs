using System.Threading.Tasks;
using DevExpress.Mvvm;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.Helpers
{
    public interface IOrderGiveHelper
    {
        Task<(OrderDto Order, OrderPaymentDto OrderPayment)> AddOrderPaymentViaCashRegistrarAsync(OrderDto order, bool printCheque, bool fullAmount, bool prepayment, ISupportServices parent);

        Task<(bool success, OrderDto order)> GiveAsync(int orderId, string contractorName, string cityName, bool useCells, ISupportServices parent, bool? white = null, bool printWarrantyCard = true);

        Task<(bool success, OrderDto order)> FastGiveAsync(int orderId, string contractorName, string cityName, ISupportServices parent);
    }
}