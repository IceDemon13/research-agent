using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.BankPayment;

namespace Telemart.Client.Data.Requests.Features.BankPayment
{
    public class QueryBankPayments : QueryEntitiesPagedRequestBase<BankPaymentDto>
    {
        public QueryBankPayments(IFilteringItem filter)
            : base(filter, ApiResources.BankPayments)
        {
        }
    }
}