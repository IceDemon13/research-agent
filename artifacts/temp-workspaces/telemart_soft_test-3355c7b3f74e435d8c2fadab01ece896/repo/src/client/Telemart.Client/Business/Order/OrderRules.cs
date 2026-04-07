using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Telemart.Client.Business.SmsTemplates;
using Telemart.Client.Business.SmsTemplates.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Store.Order;

namespace Telemart.Client.Business.Order
{
    public sealed class OrderRules : IOrderRules
    {
        private const int MaxSnLength = 100;
        private const int MinSnLength = 5;
        private const string SnValidationRegex = @"^[A-Z0-9-./\s:_]*$";

        private readonly IDictionaries _dictionaries;
        private readonly IWebClient _webClient;

        public OrderRules(IDictionaries dictionaries, IWebClient webClient)
        {
            _dictionaries = dictionaries;
            _webClient = webClient;
        }

        public IReadOnlyCollection<ISmsTemplate> GetSmsTemplates(
            int orderId,
            decimal toPayUah,
            Payment paymentType,
            Subdivision subdivision,
            int? legalEntityId,
            bool oldClient,
            int orderStateId)
        {
            SmsTemplate[] smsTemplates = _dictionaries.GetItems<SmsTemplate>().ToArray();

            List<ISmsTemplate> messageTemplates = new List<ISmsTemplate>();

            IOrderedEnumerable<SmsTemplate> smsTemplatesToProcess = smsTemplates
                .Where(x => x.ShowInOrder && (x.LegalEntityId == null || legalEntityId == null || legalEntityId == x.LegalEntityId))
                .OrderBy(x => x.Position);

            foreach (SmsTemplate smsTemplate in smsTemplatesToProcess)
            {
                switch (smsTemplate.Id)
                {
                    case SmsTemplate.CouldNotContactId:
                        messageTemplates.Add(new CouldNotContactSmsTemplate(orderId, subdivision, smsTemplate));
                        break;
                    case SmsTemplate.OrderNumberId:
                        messageTemplates.Add(new OrderNumberSmsTemplate(orderId, smsTemplate));
                        break;
                    case SmsTemplate.OrderNotWhitePaymentId:
                        messageTemplates.Add(new RequisitesSmsTemplate(orderId, paymentType, toPayUah, smsTemplate));
                        break;
                    case SmsTemplate.OrderWhitePaymentId:
                        messageTemplates.Add(new WhiteRequisitesSmsTemplate(orderId, paymentType, toPayUah, smsTemplate));
                        break;
                    case SmsTemplate.MonoPayOrderPrepaymentId:
                        messageTemplates.Add(new MonoPayPrepaymentSmsTemplate(paymentType.Id, orderId, orderStateId, smsTemplate));
                        break;
                    case SmsTemplate.OrderCancelId:
                        messageTemplates.Add(new OrderCancellationTemplate(orderId, smsTemplate));
                        break;
                    case SmsTemplate.OrderNotWhiteTuzPaymentId:
                        messageTemplates.Add(new RequisitesTuzSmsTemplate(orderId, paymentType, toPayUah, smsTemplate));
                        break;
                    case SmsTemplate.PrivatPartialOrderPrepaymentId:
                        messageTemplates.Add(new PrivatPartialPayPrepaymentSmsTemplate(paymentType.Id, orderId, orderStateId, smsTemplate));
                        break;
                    case SmsTemplate.PrivatCreditOrderPrepaymentId:
                        messageTemplates.Add(new PrivatCreditPrepaymentSmsTemplate(paymentType.Id, orderId, orderStateId, smsTemplate));
                        break;
                    case SmsTemplate.NovaPayOrderPrepaymentId:
                        messageTemplates.Add(new NovaPayPrepaymentSmsTemplate(paymentType.Id, orderId, orderStateId, smsTemplate));
                        break;
                    case SmsTemplate.NovaPayOrderPrepaymentNovakLegalEntityId:
                        messageTemplates.Add(new NovaPayNovakLegalEntityPrepaymentSmsTemplate(paymentType.Id, orderId, orderStateId, smsTemplate));
                        break;
                    case SmsTemplate.RelevanceClarificationId:
                        messageTemplates.Add(new RelevanceСlarificationSmsTemplate(smsTemplate));
                        break;
                    case SmsTemplate.PortmoneOrderPrepaymentId:
                        messageTemplates.Add(new PortmonePrepaymentSmsTemplate(paymentType.Id, orderId, orderStateId, smsTemplate));
                        break;
                    case SmsTemplate.NovaPayOrderPrepaymentTkachLegalEntityId:
                        messageTemplates.Add(new NovaPayTkachLegalEntityPrepaymentSmsTemplate(paymentType.Id, orderId, orderStateId, smsTemplate));
                        break;
                }
            }

            return messageTemplates.Where(x => x is not IBusinessOperationSmsTemplate smsTemplate || _webClient.IsOperationAllowed(smsTemplate.Operation)).ToArray();
        }

        public bool IsOrderCanBeChanged(OrderViewModel order)
        {
            return (order.State == OrderStatus.Received ||
                    (_webClient.IsOperationAllowed(BusinessOperation.OrderEditConfirmed)
                     && order.SelectedWarehouse != null
                     && _webClient.AuthenticatedEmployee.AllowWarehouses.Contains(order.SelectedWarehouse.Id)
                     && order.SelectedCarryType?.Id == CarryType.PickupId
                     && !Payment.IsCreditPayment(order.SelectedPayment?.Id)
                     && (order.State == OrderStatus.Confirmed
                         || order.State == OrderStatus.Packed))) && (order.LockerId == null || order.LockerId == _webClient.AuthenticatedEmployee.Id);
        }

        public bool IsOurBarcode(string barcode)
        {
            return new ProductBarcode(barcode).IsOur;
        }

        public bool IsProductPriceCanBeChanged(OrderProductViewModel orderProduct)
        {
            return orderProduct.State.AllowEditPrice
                && !orderProduct.IsGift && orderProduct.Product.TypeId != ProductType.GuestProductId;
        }

        public bool IsProductQuantityCanBeChanged(OrderProductViewModel orderProduct)
        {
            return orderProduct.State != null && orderProduct.State.AllowEditQuantity && !orderProduct.Source.Real;
        }

        public bool NeedCalcProductDateX(OrderStatus orderState, IOrderProduct orderProduct)
        {
            OrderProductSource source = orderProduct.Source;

            bool calcProductDateX = orderState.CalcProductDateX &&
                orderProduct.State.CalcProductDateX &&
                source.SourceDate.HasValue &&
                source.WarehouseId > 0;

            return calcProductDateX;
        }

        public bool NeedCheckGifts(Subdivision subdivision)
        {
            return subdivision.Id == Subdivision.Telemart.Id || subdivision.Id == Subdivision.Retail.Id;
        }

        public string ValidateSerialNumber(string serialNumber, List<ProductSnLengthDto> productSerialNumberLength, bool ignoreLengthValidation = false)
        {
            if (serialNumber is null)
            {
                return "SN не может быть пустым";
            }

            string trimSerialNumber = serialNumber.Trim();

            if (!Regex.IsMatch(trimSerialNumber, SnValidationRegex))
            {
                return "Неверный формат SN";
            }

            if (string.IsNullOrEmpty(trimSerialNumber) || trimSerialNumber.Length < MinSnLength || trimSerialNumber.Length > MaxSnLength)
            {
                return $"Длина SN должна быть от {MinSnLength} до {MaxSnLength} символов";
            }

            if (productSerialNumberLength?.Any() != true)
            {
                return null;
            }

            int[] length = productSerialNumberLength.Select(x => x.Length).Distinct().ToArray();

            if (!ignoreLengthValidation && !length.Any(x => serialNumber.Length >= x - 1 && serialNumber.Length <= x + 1))
            {
                return $"Возможные варианты длины SN: {string.Join(", ", length)} симв.";
            }

            return null;
        }

        public string ValidateOurBarcode(string ourBarcode)
        {
            if (string.IsNullOrWhiteSpace(ourBarcode))
            {
                return "ШК не может быть пустым";
            }

            if (!IsOurBarcode(ourBarcode))
            {
                return "Неверный ШК";
            }

            return null;
        }
    }
}