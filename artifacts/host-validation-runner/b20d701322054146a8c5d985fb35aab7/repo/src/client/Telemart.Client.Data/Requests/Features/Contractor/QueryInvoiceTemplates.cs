using System;
using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor
{
    public sealed class QueryInvoiceTemplates : QueryEntityRequestBase<List<InvoiceTemplateDto>>
    {
        public QueryInvoiceTemplates(int id, DateTime? dateFrom)
            : base(ApiResources.Contractors, id, "invoice", "templates")
        {
            if (dateFrom.HasValue)
            {
                UrlParameters = new (string Name, object Value)[] { ("dateFrom", dateFrom.Value.ToString("s")) };
            }
        }
    }
}