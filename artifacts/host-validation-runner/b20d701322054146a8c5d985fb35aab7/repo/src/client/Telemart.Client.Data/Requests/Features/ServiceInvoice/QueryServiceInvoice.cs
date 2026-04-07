using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceInvoice
{
    public sealed class QueryServiceInvoice : QueryEntityRequestBase<ServiceInvoiceDto>
    {
        public QueryServiceInvoice(int id)
            : base(ApiResources.ServiceInvoices, id)
        {
        }
    }
}