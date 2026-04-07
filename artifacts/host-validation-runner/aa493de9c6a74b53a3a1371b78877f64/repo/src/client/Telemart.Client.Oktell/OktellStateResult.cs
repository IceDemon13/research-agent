using System;

namespace Telemart.Client.Oktell
{
    public class OktellStateResult : OktellResult
    {
        public OktellStateResult(string message, bool isError)
            : base(message, isError)
        {
            State = OktellState.Disconnected;
        }

        public OktellStateResult(OktellState state, string phone, DateTime? startTime, int? lineNumber, OktellType type, string name)
        {
            State = state;
            StartTime = startTime;
            LineNumber = lineNumber;
            Type = type;
            Name = name;
            Phone = phone;
        }

        public OktellState State { get; }

        public DateTime? StartTime { get; }

        public int? LineNumber { get; }

        public string Phone { get; }

        public OktellType Type { get; }

        public string Name { get; }
    }
}