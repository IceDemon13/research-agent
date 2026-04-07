using System.Collections.Generic;

namespace Telemart.Client.ViewModels.Store.Order.OrderPaymentInfo
{
    public interface IOrderPaymentInfo
    {
        int Id { get; }

        int? PackageDeliveryCost { get; }

        int? MinPriceFreeDelivery { get; }

        decimal? MoneyBackAmount { get; }

        IEnumerable<IOrderPaymentInfoProduct> GetOrderProducts();

        IEnumerable<IOrderPayment> GetOrderPayments();

        int GetBonusesQuantity();

        int GetBonusesToChargeQuantity();
    }
}