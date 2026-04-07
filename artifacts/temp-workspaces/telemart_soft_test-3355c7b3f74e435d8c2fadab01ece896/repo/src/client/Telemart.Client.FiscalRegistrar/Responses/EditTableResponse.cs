using Telemart.Client.FiscalRegistrar.Responses.Base;

namespace Telemart.Client.FiscalRegistrar.Responses
{
    public class EditTableResponse : FiscalRegistrarResponseBase
    {
        public EditTableResponse(string raw)
            : base(raw)
        {
        }

        public EditTableResponse(int statusCode, string reason)
            : base(statusCode, reason)
        {
        }
    }
}
