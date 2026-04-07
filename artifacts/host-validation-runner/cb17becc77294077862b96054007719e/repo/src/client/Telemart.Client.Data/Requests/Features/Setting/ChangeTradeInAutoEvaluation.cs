using Telemart.Client.Data.Requests.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.Setting
{
    public sealed class ChangeTradeInAutoEvaluation : UpdateEntityRequestBase<Result, object>
    {
        public ChangeTradeInAutoEvaluation()
            : base(null, ApiResources.Settings, "change_trade_in_auto_evaluation")
        {
        }
    }
}