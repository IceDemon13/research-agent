using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Telemart.Client.FiscalRegistrar.Responses.Base;

namespace Telemart.Client.FiscalRegistrar.Responses
{
    public class CashStockResponse : FiscalRegistrarResponseBase
    {
        public CashStockResponse(string raw)
            : base(raw)
        {
            if (IsOk && ResponseObject is JArray jArray)
            {
                CashStocks = jArray.ToObject<CashStock[]>();
            }
        }

        public CashStockResponse(int statusCode, string reason)
            : base(statusCode, reason)
        {
        }

        public IReadOnlyCollection<CashStock> CashStocks { get; }
    }
}
