using Telemart.Client.Core.Update;

namespace Telemart.Client.Common.Messages
{
    public sealed class UpdateAvailableMessage
    {
        public UpdateAvailableMessage(WinCheckForUpdateResult updateResult)
        {
            UpdateResult = updateResult;
        }

        public WinCheckForUpdateResult UpdateResult { get; }
    }
}