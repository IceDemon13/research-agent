using System.Collections.Generic;
using System.Net.Http;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice
{
    // TODO: Переделать метод на action
    public sealed class AssignInvoiceProductsToCurrentUser : RestClientGatewayRequestBase<InvoiceDto>
    {
        public AssignInvoiceProductsToCurrentUser(int invoiceId, IEnumerable<int> invoiceProductsIds)
            : base(HttpMethod.Post)
        {
            PathParameters = new object[] { ApiResources.Invoices, invoiceId, "products", "employee" };

            Body = invoiceProductsIds;
        }
    }
}