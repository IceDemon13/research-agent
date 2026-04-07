using System;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.Barcode;

namespace Telemart.Client.ViewModels.Common.RecognizeServiceBarcode
{
    public class RecognizeServiceBarcodeViewModel : ViewModelBase
    {
        public RecognizeServiceBarcodeViewModel(ILogger<RecognizeServiceBarcodeViewModel> logger)
        {
            RecognizeBarcodeCommand = new DelegateCommand<string>(RecognizeBarcode);

            Logger = logger;
        }

        public event EventHandler OnFinishCommand;

        public event EventHandler<RecognizeServiceBarcodeResultEventArgs> OnFinished;

        public event EventHandler OnStarted;

        #region Commands

        public IDelegateCommand RecognizeBarcodeCommand { get; }

        #endregion

        #region INPC

        public string BarcodeText
        {
            get { return GetProperty(() => BarcodeText); }
            set { SetProperty(() => BarcodeText, value); }
        }

        #endregion

        private ILogger<RecognizeServiceBarcodeViewModel> Logger { get; }

        private void FireOnFinished(RecognizeServiceBarcodeResultEventArgs args)
        {
            OnFinished?.Invoke(this, args);
        }

        private void FireOnStarted()
        {
            OnStarted?.Invoke(this, EventArgs.Empty);
        }

        private void RecognizeBarcode(string barcodeText)
        {
            if (string.IsNullOrEmpty(barcodeText))
            {
                return;
            }

            try
            {
                if (string.Equals(barcodeText, BarcodeConstants.CmdFinish, StringComparison.Ordinal))
                {
                    OnFinishCommand?.Invoke(this, EventArgs.Empty);
                    return;
                }

                FireOnStarted();

                OurServiceBarcode barcode = new OurServiceBarcode(barcodeText);

                RecognizeServiceBarcodeResultEventArgs eventArgs = barcode.IsValid
                    ? RecognizeServiceBarcodeResultEventArgs.Found(barcode.ServiceRequestId, barcodeText)
                    : RecognizeServiceBarcodeResultEventArgs.Error(barcodeText, "Неверный формат ШК");

                FireOnFinished(eventArgs);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to recognize service request by barcode");
                FireOnFinished(RecognizeServiceBarcodeResultEventArgs.Error(barcodeText, "Ошибка при распознавании ШК"));
            }
            finally
            {
                BarcodeText = string.Empty;
            }
        }
    }
}