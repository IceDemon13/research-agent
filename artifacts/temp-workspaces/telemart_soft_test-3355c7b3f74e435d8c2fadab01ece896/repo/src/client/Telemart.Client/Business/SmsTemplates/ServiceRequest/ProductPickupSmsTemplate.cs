using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using SmartFormat;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Business.SmsTemplates.ServiceRequest
{
    public sealed class ProductPickupSmsTemplate : ISmsTemplate
    {
        public ProductPickupSmsTemplate(int serviceRequestId, string warehouseAddress, string warehouseInfo, SmsTemplate template)
        {
            ServiceRequestId = serviceRequestId;

            TemplateId = SmsTemplate.ServiceRequestProductPickupId;

            DisplayName = template.Name;

            Data = new
            {
                ServiceRequestId = serviceRequestId,
                WarehouseAddress = warehouseAddress,
                WarehouseInfo = warehouseInfo
            };

            SmsText = Smart.Format(template.SmsText, Data);
            ViberText = Smart.Format(template.ViberText, Data);
        }

        public int ServiceRequestId { get; set; }

        public object Data { get; }

        public int? TemplateId { get; set; }

        public string DisplayName { get; }

        public string SmsText { get; }

        public string ViberText { get; }

        public IEnumerable<string> Validate()
        {
            yield break;
        }
    }
}