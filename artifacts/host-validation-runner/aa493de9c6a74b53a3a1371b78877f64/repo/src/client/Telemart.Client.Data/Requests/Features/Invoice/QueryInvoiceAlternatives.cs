using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice
{
    public class QueryInvoiceAlternatives : QueryEntityRequestBase<List<ProductAlternativeDto>>
    {
        public QueryInvoiceAlternatives(int id)
            : base(ApiResources.Invoices, id, "products", "alternatives")
        {
        }
    }
}