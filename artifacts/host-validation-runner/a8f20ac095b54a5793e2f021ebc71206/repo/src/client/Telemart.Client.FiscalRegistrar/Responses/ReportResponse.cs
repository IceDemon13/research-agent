using Telemart.Client.FiscalRegistrar.Responses.Base;

namespace Telemart.Client.FiscalRegistrar.Responses
{
    public class ReportResponse : FiscalRegistrarResponseBase
    {
        public ReportResponse(string raw)
            : base(raw)
        {
        }

        public ReportResponse(int statusCode, string reason)
            : base(statusCode, reason)
        {
        }
    }
}
