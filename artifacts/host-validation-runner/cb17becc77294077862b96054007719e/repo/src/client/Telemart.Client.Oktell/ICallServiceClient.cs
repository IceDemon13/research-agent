using System.Diagnostics.SymbolStore;
using System.Threading.Tasks;

namespace Telemart.Client.Oktell
{
    public interface ICallServiceClient
    {
        Task<OktellResult> CallAsync(string number);

        bool CanCall();

        Task<OktellStateResult> GetStateAsync();

        Task<OktellResult> EndCallAsync();

        bool CanEndCall();

        Task<OktellResult> CancelCallAsync();

        bool CanCancelCall();

        Task<OktellResult> ApplyCallAsync();

        bool CanApplyCall();
    }
}