using System;
using System.Threading;
using System.Threading.Tasks;
using Telemart.Client.FiscalRegistrar.Abstraction;
using Telemart.Client.TransferObjects.FiscalRegistrar;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.FiscalRegistrar
{
    public interface IFiscalRegistrarClientFactory
    {
        Task<Result<IFiscalRegistrarClient>> CreateAsync(CancellationToken cancellationToken);

        Task<IFiscalRegistrarClient> CreateAsync(FiscalRegistrarSettingsDto settings, CancellationToken cancellationToken);

        Task<IFiscalRegistrarClient> CreateAsync(Type type, FiscalRegistrarSettingsDto settings, CancellationToken cancellationToken);
    }
}