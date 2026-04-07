using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using SmartFormat;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.Business.SmsTemplates.ServiceRequest
{
    public sealed class RefundPickupSmsTemplate : ISmsTemplate
    {
        public RefundPickupSmsTemplate(int serviceRequestId, string warehouseAddress, string warehouseInfo, SmsTemplate template)
        {
            DisplayName = template.Name;

            ServiceRequestId = serviceRequestId;

            TemplateId = SmsTemplate.ServiceRequestRefundId;

            Data = new
            {
                ServiceRequestId = serviceRequestId,
                WarehouseAddress = warehouseAddress,
                WarehouseInfo = warehouseInfo
            };

            SmsText = Smart.Format(template.SmsText, Data);
            ViberText = Smart.Format(template.ViberText, Data);
        }

        public int ServiceRequestId { get; }

        public object Data { get; }

        public int? TemplateId { get; }

        public string DisplayName { get; }

        public string SmsText { get; }

        public string ViberText { get; }

        public IEnumerable<string> Validate()
        {
            yield break;
        }
    }
}