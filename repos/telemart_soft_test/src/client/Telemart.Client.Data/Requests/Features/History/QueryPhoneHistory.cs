using System.Linq;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.History
{
    public class QueryPhoneHistory : QueryEntityRequestBase<PhoneHistoryResultDto>
    {
        public QueryPhoneHistory(params string[] phones)
            : base(ApiResources.History, "phone")
        {
            UrlParameters = phones.Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => ("phone", (object)x));
        }
    }
}