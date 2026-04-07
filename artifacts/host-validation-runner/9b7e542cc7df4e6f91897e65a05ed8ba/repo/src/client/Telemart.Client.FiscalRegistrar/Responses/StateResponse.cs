using Telemart.Client.FiscalRegistrar.Responses.Base;

namespace Telemart.Client.FiscalRegistrar.Responses
{
    public sealed class StateResponse : FiscalRegistrarResponseBase
    {
        public StateResponse(string raw)
            : base(raw)
        {
        }

        public StateResponse(int statusCode, string reason)
            : base(statusCode, reason)
        {
        }
    }
}
