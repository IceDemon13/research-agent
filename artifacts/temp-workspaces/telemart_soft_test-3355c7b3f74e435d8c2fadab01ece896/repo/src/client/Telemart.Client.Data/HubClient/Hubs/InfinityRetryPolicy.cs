using System;
using Microsoft.AspNetCore.SignalR.Client;

namespace Telemart.Client.Data.HubClient.Hubs
{
    public class InfinityRetryPolicy : IRetryPolicy
    {
        private readonly TimeSpan[] intervals =
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(7),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(20),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(1)
        };

        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            return intervals[Math.Min(intervals.Length - 1, retryContext.PreviousRetryCount)];
        }
    }
}