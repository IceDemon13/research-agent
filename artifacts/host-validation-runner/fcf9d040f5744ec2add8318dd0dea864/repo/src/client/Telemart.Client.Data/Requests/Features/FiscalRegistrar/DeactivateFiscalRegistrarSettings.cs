using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.FiscalRegistrar
{
    public sealed class DeactivateFiscalRegistrarSettings : CallEntityActionRequestBase<Result>
    {
        public DeactivateFiscalRegistrarSettings(int id)
            : base(id, $"{ApiResources.Fiscal}/settings", "disable")
        {
        }
    }
}