using System;
using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Common
{
    public sealed class BarcodeRecognitionViewItem : BindableBase
    {
        public BarcodeRecognitionViewItem(RecognizeBarcodeMessageType result, string message, DateTime time)
        {
            Result = result;
            Time = time;
            Message = message;
        }

        public BarcodeRecognitionViewItem()
        {
        }

        public RecognizeBarcodeMessageType Result
        {
            get { return GetProperty(() => Result); }
            set { SetProperty(() => Result, value); }
        }

        public DateTime Time
        {
            get { return GetProperty(() => Time); }
            set { SetProperty(() => Time, value); }
        }

        public string Message
        {
            get { return GetProperty(() => Message); }
            set { SetProperty(() => Message, value); }
        }

        public Action ActionWarning
        {
            get { return GetProperty(() => ActionWarning); }
            set { SetProperty(() => ActionWarning, value); }
        }
    }
}