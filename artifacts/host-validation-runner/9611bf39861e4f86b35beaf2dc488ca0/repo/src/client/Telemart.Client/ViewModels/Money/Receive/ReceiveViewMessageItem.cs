using System;
using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Money.Receive
{
    public sealed class ReceiveViewMessageItem : BindableBase
    {
        public ReceiveViewMessageItem(int number, string message)
        {
            Message = $"Строка {number:D}. {message}";
            Time = DateTime.Now;
        }

        public DateTime Time
        {
            get { return GetProperty(() => Time); }
            private set { SetProperty(() => Time, value); }
        }

        public string Message
        {
            get { return GetProperty(() => Message); }
            private set { SetProperty(() => Message, value); }
        }
    }
}
