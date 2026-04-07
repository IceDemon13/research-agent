using System.Collections.Generic;
using System.Threading.Tasks;
using Telemart.Client.TransferObjects.PosTerminal;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Validators
{
    public interface IPosSettingsValidator
    {
        Task<Result<IReadOnlyCollection<PosSettingsDto>>> ValidateAsync(IReadOnlyCollection<PosSettingsDto> posSettings);
    }
}