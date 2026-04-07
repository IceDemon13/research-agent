using System;

namespace Telemart.Client.ViewModels.Dialogs
{
   public class ChangeDateTimeParameter
    {
        public ChangeDateTimeParameter(DateTime? oldDateTime, string title, Func<DateTime?, string> isValidFunc, int? orderStateId)
        {
            OldDateTime = oldDateTime;
            Title = title;
            IsValidFunc = isValidFunc;
            OrderStateId = orderStateId;
        }

        public ChangeDateTimeParameter(DateTime? oldDateTime, string title, Func<DateTime?, string> isValidFunc, int? orderStateId, bool isAdditionalDate, bool isAssemblyDate)
            : this(oldDateTime, title, isValidFunc, orderStateId)
        {
            IsAdditionalDate = isAdditionalDate;
            IsAssemblyServiceDate = isAssemblyDate;
        }

        public DateTime? OldDateTime { get; }

        public string Title { get; }

        public Func<DateTime?, string> IsValidFunc { get; }

        public int? OrderStateId { get; }

        public bool IsAdditionalDate { get; }

        public bool IsAssemblyServiceDate { get; }
    }
}