using System;

namespace Telemart.Client.ViewModels.Dialogs
{
    public sealed class ChangeDateTimeResult
    {
        public ChangeDateTimeResult(DateTime dateTime, int? orderStateChangeReasonId, string comment)
        {
            NewDate = dateTime;
            OrderStateChangeReasonId = orderStateChangeReasonId;
            Comment = comment;
        }

        public DateTime NewDate { get; }

        public int? OrderStateChangeReasonId { get; }

        public string Comment { get; }
    }
}