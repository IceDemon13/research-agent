using Telemart.Client.PosTerminal.Ingenico;

namespace Telemart.Client.PosTerminal.PrivatBank
{
    public interface IPrivatBankPosTerminalClient : IPosTerminalClient
    {
        void Kill();
    }
}