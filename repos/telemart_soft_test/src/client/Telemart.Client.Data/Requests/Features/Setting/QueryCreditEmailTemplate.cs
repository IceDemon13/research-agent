using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Setting
{
    public class QueryCreditEmailTemplate : QueryEntityRequestBase<string>
    {
        public QueryCreditEmailTemplate()
            : base(ApiResources.Settings, "credit_email_template")
        {
        }
    }
}