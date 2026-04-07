using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using SmartFormat;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Business.SmsTemplates.ServiceRequest
{
    public sealed class NotReachedSmsTemplate : ISmsTemplate
    {
        public NotReachedSmsTemplate(int serviceRequestId, SmsTemplate template)
        {
            ServiceRequestId = serviceRequestId;

            DisplayName = template.Name;

            TemplateId = SmsTemplate.ServiceRequestNotReachedId;

            Data = new { ServiceRequestId = serviceRequestId };

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