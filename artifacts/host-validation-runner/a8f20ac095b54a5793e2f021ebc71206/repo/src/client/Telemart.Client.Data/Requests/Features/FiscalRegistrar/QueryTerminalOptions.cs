using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.PosTerminal;

namespace Telemart.Client.Data.Requests.Features.FiscalRegistrar
{
    public sealed class QueryTerminalOptions : QueryEntityRequestBase<TerminalOptionsDto>
    {
        public QueryTerminalOptions()
            : base(ApiResources.Pos, "terminal_options")
        {
        }
    }
}