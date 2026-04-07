using System.Collections.Generic;
using Telemart.Client.Business.SmsTemplates;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Store.Order;

namespace Telemart.Client.Business.Order
{
    public interface IOrderRules
    {
        IReadOnlyCollection<ISmsTemplate> GetSmsTemplates(int orderId, decimal toPayUah, Payment paymentType, Subdivision subdivision, int? legalEntityId, bool oldClient, int orderStateId);

        bool IsOrderCanBeChanged(OrderViewModel order);

        bool IsOurBarcode(string barcode);

        bool IsProductPriceCanBeChanged(OrderProductViewModel orderProduct);

        bool IsProductQuantityCanBeChanged(OrderProductViewModel orderProduct);

        bool NeedCalcProductDateX(OrderStatus orderState, IOrderProduct orderProduct);

        bool NeedCheckGifts(Subdivision subdivision);

        string ValidateSerialNumber(string serialNumber, List<ProductSnLengthDto> productSerialNumberLength, bool ignoreLengthValidation = false);

        string ValidateOurBarcode(string ourBarcode);
    }
}