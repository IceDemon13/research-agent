using System.Collections.Generic;
using Telemart.Client.Business.SmsTemplates;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels
{
    public sealed class SendSmsParameter
    {
        public SendSmsParameter(
            int orderId,
            int? serviceRequestId,
            int? priceTypeId,
            Subdivision subdivision,
            string phone,
            string phone2 = null,
            IReadOnlyCollection<ISmsTemplate> templates = null,
            decimal? amount = null)
        {
            OrderId = orderId;
            ServiceRequestId = serviceRequestId;
            Subdivision = subdivision;
            Phone = phone;
            Phone2 = phone2;
            Templates = templates;
            Amount = amount;
            PriceTypeId = priceTypeId;
        }

        public int OrderId { get; }

        public int? ServiceRequestId { get; }

        public int? PriceTypeId { get; }

        public Subdivision Subdivision { get; }

        public string Phone { get; }

        public string Phone2 { get; }

        public decimal? Amount { get; }

        public IReadOnlyCollection<ISmsTemplate> Templates { get; }
    }
}