using System.Threading.Tasks;
using Telemart.Client.Core.Helpers;

namespace Telemart.Client.Oktell
{
    public class AsteriskClient: ICallServiceClient
    {
        public Task<OktellResult> CallAsync(string number)
        {
            ProcessHelper.Start($"sip:{number}");

            return Task.FromResult(new OktellResult());
        }

        public bool CanCall() => true;

        public Task<OktellStateResult> GetStateAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<OktellResult> EndCallAsync()
        {
            throw new System.NotImplementedException();
        }

        public bool CanEndCall() => false;

        public Task<OktellResult> CancelCallAsync()
        {
            throw new System.NotImplementedException();
        }

        public bool CanCancelCall() => false;

        public Task<OktellResult> ApplyCallAsync()
        {
            throw new System.NotImplementedException();
        }

        public bool CanApplyCall() => false;
    }
}