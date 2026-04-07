using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Sms
{
    public class QuerySmsTemplates : QueryEntitiesRequestBase<SmsTemplateDto>
    {
        public QuerySmsTemplates()
            : base("sms/templates")
        {
        }
    }
}
