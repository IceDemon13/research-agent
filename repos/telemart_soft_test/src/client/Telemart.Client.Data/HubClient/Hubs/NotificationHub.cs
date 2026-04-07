using Microsoft.Extensions.Logging;
using Telemart.Client.Core.Update;
using Telemart.Client.Data.Authentication;
using Telemart.Client.Data.HubClient.Base;
using Telemart.Client.Data.Options;

namespace Telemart.Client.Data.HubClient.Hubs
{
    public sealed class NotificationHub : HubClientBase<NotificationHub>
    {
        public NotificationHub(
            MainServiceOptions mainServiceOptions,
            IAuthenticationManager authenticationManager,
            IUpdateManager updateManager,
            ILogger<NotificationHub> logger)
            : base(mainServiceOptions, authenticationManager, updateManager, logger)
        {
        }

        public const string GetNotificationMethod = "GetNotification";
        public const string NotifyAllMethod = "NotifyAll";
        public const string CallEventMethodName = "CallEvent";

        protected override string GetHubName() => "Notification";
    }
}