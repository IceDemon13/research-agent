using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Sms
{
    public class QuerySmsTypes : QueryEntitiesRequestBase<SmsTypeDto>
    {
        public QuerySmsTypes()
            : base("sms/types")
        {
        }
    }
}