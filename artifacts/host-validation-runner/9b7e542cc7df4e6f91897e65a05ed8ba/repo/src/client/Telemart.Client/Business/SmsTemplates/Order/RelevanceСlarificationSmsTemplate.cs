using System.Collections.Generic;
using SmartFormat;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Business.SmsTemplates.Order
{
    public class RelevanceСlarificationSmsTemplate : ISmsTemplate
    {
        public RelevanceСlarificationSmsTemplate(SmsTemplate template)
        {
            TemplateId = SmsTemplate.RelevanceClarificationId;

            DisplayName = template.Name;

            SmsText = template.SmsText;
            ViberText = template.ViberText;
        }

        public string DisplayName { get; }

        public string SmsText { get; }

        public string ViberText { get; }

        public object Data { get; }

        public int? TemplateId { get; }

        public IEnumerable<string> Validate()
        {
            yield break;
        }
    }
}