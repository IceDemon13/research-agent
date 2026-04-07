using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice
{
    public sealed class QueryInvoiceProductAttributes : QueryEntityRequestBase<List<ProductAttributesDto>>
    {
        public QueryInvoiceProductAttributes(int id)
            : base(ApiResources.Invoices, id, "products", "attributes")
        {
        }
    }
}