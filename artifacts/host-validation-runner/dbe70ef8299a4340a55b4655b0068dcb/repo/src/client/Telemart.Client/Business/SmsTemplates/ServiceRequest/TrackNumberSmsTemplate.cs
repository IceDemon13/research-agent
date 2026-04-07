using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using SmartFormat;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Business.SmsTemplates.ServiceRequest
{
    public sealed class TrackNumberSmsTemplate : ISmsTemplate
    {
        public TrackNumberSmsTemplate(int serviceRequestId, string trackNumber, SmsTemplate template)
        {
            ServiceRequestId = serviceRequestId;
            TrackNumber = trackNumber;

            TemplateId = SmsTemplate.ServiceRequestTtnId;

            DisplayName = template.Name;

            Data = new { ServiceRequestId = serviceRequestId, TrackNumber = trackNumber };

            SmsText = Smart.Format(template.SmsText, Data);
            ViberText = Smart.Format(template.ViberText, Data);
        }

        public int ServiceRequestId { get; }

        public object Data { get; }

        public int? TemplateId { get; }

        public string TrackNumber { get; }

        public string DisplayName { get; }

        public string SmsText { get; }

        public string ViberText { get; }

        public IEnumerable<string> Validate()
        {
            yield break;
        }
    }
}