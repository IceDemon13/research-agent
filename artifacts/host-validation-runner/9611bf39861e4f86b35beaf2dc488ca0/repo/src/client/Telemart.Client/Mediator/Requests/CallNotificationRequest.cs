using System;
using MediatR;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Mediator.Requests
{
    public class CallNotificationRequest : INotification, IEquatable<CallNotificationRequest>
    {
        public string Phone { get; init; }

        public CallNotificationState State { get; init; }

        public CallNotificationType Type { get; init; }

        public string Ivr { get; init; }

        public bool Equals(CallNotificationRequest other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Phone == other.Phone && State == other.State && Type == other.Type && Ivr == other.Ivr;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((CallNotificationRequest)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Phone, (int)State, (int)Type, Ivr);
        }
    }
}