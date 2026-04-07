using System;

namespace Telemart.Client.Business.Schedule
{
    public sealed class CurrentTimeProvider : ITimeProvider
    {
        public DateTime Now => DateTime.Now;
    }
}