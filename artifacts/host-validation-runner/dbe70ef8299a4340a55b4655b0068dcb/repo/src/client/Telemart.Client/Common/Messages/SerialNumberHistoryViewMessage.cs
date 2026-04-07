using System;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.Common.Messages
{
    public class SerialNumberHistoryViewMessage : EditorParameter
    {
        public SerialNumberHistoryViewMessage(string serialNumber)
            : base(serialNumber?.GetHashCode() ?? 0)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
            {
                throw new ArgumentException("Serial number is empty", nameof(serialNumber));
            }

            SerialNumber = serialNumber;
        }

        public string SerialNumber { get; }
    }
}