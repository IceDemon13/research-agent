using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.FiscalDocument;

namespace Telemart.Client.Data.Requests.Features.FiscalDocument
{
    public class QueryFiscalDocumentPaymentTypes : QueryEntitiesRequestBase<FiscalDocumentPaymentTypeDto>
    {
        public QueryFiscalDocumentPaymentTypes()
            : base($"{ApiResources.FiscalDocument}/paymenttype")
        {
        }
    }
}
