using System.Collections.Generic;

namespace Telemart.Client.Business.SmsTemplates
{
    public interface ISmsTemplate
    {
        string DisplayName { get; }

        string SmsText { get; }

        string ViberText { get; }

        object Data { get; }

        int? TemplateId { get; }

        IEnumerable<string> Validate();
    }
}