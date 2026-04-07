using System;

namespace Telemart.Client.Common.Messages
{
    public class LockTimeoutChangedMessage
    {
        public LockTimeoutChangedMessage(TimeSpan? timeOut)
        {
            TimeOut = timeOut;
        }

        public TimeSpan? TimeOut { get; }
    }
}