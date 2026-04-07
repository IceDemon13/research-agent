using System;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Common
{
    public abstract class RecognizeBarcodeViewModelBase<TArgs> : TelemartViewModelBase
        where TArgs : RecognizeBarcodeResultEventArgsBase
    {
        public RecognizeBarcodeViewModelBase(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            RecognizeBarcodeCommand = new AsyncCommand<string>(RecognizeBarcodeAsync);

            RecognitionViewItems = new ObservableRangeCollection<BarcodeRecognitionViewItem>();
        }

        public RecognizeBarcodeViewModelBase()
        {
        }

        public event EventHandler OnFinishCommand = (sender, e) => { };

        public event EventHandler<TArgs> OnFinished = (sender, e) => { };

        public event EventHandler OnStarted = (sender, e) => { };

        #region Commands

        public IAsyncCommand RecognizeBarcodeCommand { get; }

        #endregion

        #region INPC

        public string BarcodeText
        {
            get { return GetProperty(() => BarcodeText); }
            set { SetProperty(() => BarcodeText, value); }
        }

        public decimal Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public bool UseQuantity
        {
            get { return GetProperty(() => UseQuantity); }
            set { SetProperty(() => UseQuantity, value); }
        }

        public ObservableRangeCollection<BarcodeRecognitionViewItem> RecognitionViewItems
        {
            get { return GetProperty(() => RecognitionViewItems); }
            set { SetProperty(() => RecognitionViewItems, value); }
        }

        public BarcodeRecognitionViewItem SelectedRecognitionItem
        {
            get { return GetProperty(() => SelectedRecognitionItem); }
            set { SetProperty(() => SelectedRecognitionItem, value); }
        }

        #endregion

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            UseQuantity = true;
            Quantity = 1;
        }

        protected virtual void FireOnFinished(TArgs args)
        {
            OnFinished.Invoke(this, args);

            if (!args.IsValid)
            {
                BarcodeRecognitionViewItem recognitionItem = new BarcodeRecognitionViewItem(RecognizeBarcodeMessageType.Error, args.ErrorText, DateTime.Now);
                RecognitionViewItems.Insert(0, recognitionItem);
                SelectedRecognitionItem = recognitionItem;
            }
        }

        protected void FireOnFinishCommand()
        {
            OnFinishCommand.Invoke(this, EventArgs.Empty);
        }

        protected void FireOnStarted()
        {
            OnStarted.Invoke(this, EventArgs.Empty);
        }

        protected abstract Task RecognizeBarcodeAsync(string barcodeText);
    }
}
