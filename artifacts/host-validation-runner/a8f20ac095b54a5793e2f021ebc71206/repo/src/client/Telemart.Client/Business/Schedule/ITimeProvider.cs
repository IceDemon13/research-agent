using System;

namespace Telemart.Client.Business.Schedule
{
    public interface ITimeProvider
    {
        DateTime Now { get; }
    }
}