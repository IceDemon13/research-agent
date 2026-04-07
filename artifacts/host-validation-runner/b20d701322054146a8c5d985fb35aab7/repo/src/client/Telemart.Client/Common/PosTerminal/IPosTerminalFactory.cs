using System.Threading.Tasks;
using Telemart.Client.Common.Settings.Equipment;
using Telemart.Client.PosTerminal.Ingenico;
using Telemart.Client.TransferObjects.PosTerminal;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Common.PosTerminal
{
    public interface IPosTerminalFactory
    {
        Task<Result<IPosTerminalClient>> CreateAsync(int legalEntityId);

        Task<IPosTerminalClient> CreateAsync(PosSettingsDto settings);

        IPosTerminalClient Create(PosType type, string ip, PosSettingsDto settings = null);
    }
}