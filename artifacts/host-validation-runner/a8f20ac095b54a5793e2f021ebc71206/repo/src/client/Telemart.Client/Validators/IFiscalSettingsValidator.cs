using System.Collections.Generic;
using System.Threading.Tasks;
using Telemart.Client.TransferObjects.FiscalRegistrar;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Validators
{
    public interface IFiscalSettingsValidator
    {
        Task<Result<IReadOnlyCollection<FiscalRegistrarSettingsDto>>> ValidateAsync(IReadOnlyCollection<FiscalRegistrarSettingsDto> posSettings);
    }
}