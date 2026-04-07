using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using SmartFormat;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Business.SmsTemplates.ServiceRequest
{
    public sealed class NewServiceRequestSmsTemplate : ISmsTemplate
    {
        public NewServiceRequestSmsTemplate(int serviceRequestId, SmsTemplate template)
        {
            ServiceRequestId = serviceRequestId;

            TemplateId = SmsTemplate.ServiceRequestNumberId;

            DisplayName = template.Name;

            Data = new { ServiceRequestId = serviceRequestId };

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