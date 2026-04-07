using Newtonsoft.Json.Linq;
using Telemart.Client.FiscalRegistrar.Responses.Base;

namespace Telemart.Client.FiscalRegistrar.Responses
{
    public sealed class PrintChequeResponse : FiscalRegistrarResponseBase
    {
        public PrintChequeResponse(string raw)
            : base(raw)
        {
            if (IsOk && ResponseObject is JObject jObject)
            {
                Id = jObject.Value<int>("id");
            }
        }

        public PrintChequeResponse(int statusCode, string reason)
            : base(statusCode, reason)
        {
        }

        public int Id { get; }
    }
}