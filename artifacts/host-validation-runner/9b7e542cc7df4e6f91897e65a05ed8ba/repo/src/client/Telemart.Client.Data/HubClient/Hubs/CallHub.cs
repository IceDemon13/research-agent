using Microsoft.Extensions.Logging;
using Telemart.Client.Core.Update;
using Telemart.Client.Data.Authentication;
using Telemart.Client.Data.HubClient.Base;
using Telemart.Client.Data.Options;

namespace Telemart.Client.Data.HubClient.Hubs
{
    public sealed class CallHub : HubClientBase<CallHub>
    {
        public CallHub(
            MainServiceOptions mainServiceOptions,
            IAuthenticationManager authenticationManager,
            IUpdateManager updateManager,
            ILogger<CallHub> logger)
            : base(mainServiceOptions, authenticationManager, updateManager, logger)
        {
        }

        public const string GetCallDependencyMethod = "GetCallDependency";

        protected override string GetHubName() => "Call";
    }
}