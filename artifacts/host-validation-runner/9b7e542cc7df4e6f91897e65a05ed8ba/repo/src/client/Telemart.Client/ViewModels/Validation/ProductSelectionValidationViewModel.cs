using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Validation
{
    public sealed class ProductSelectionValidationViewModel : TelemartDialogViewModelBase
    {
        private const string OkString = "Да";
        private const int TotalSecondsToWait = 5;

        private DispatcherTimer countdownTimer;
        private int secondsLeft;

        public ProductSelectionValidationViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            HandleUnloadedCommand = new DelegateCommand(HandleUnloaded);
            NoCommand = new DelegateCommand(HandleNo);
            HandlePreviewKeyDownCommand = new DelegateCommand<KeyEventArgs>(HandlePreviewKeyDown);
        }

        public ProductSelectionValidationViewModel()
        {
        }

        #region Commands

        public IDelegateCommand NoCommand { get; }

        public IDelegateCommand HandlePreviewKeyDownCommand { get; }

        public IDelegateCommand HandleUnloadedCommand { get; }

        #endregion

        public bool HasErrors
        {
            get { return GetProperty(() => HasErrors); }
            private set { SetProperty(() => HasErrors, value); }
        }

        public bool HasValidationItems
        {
            get { return GetProperty(() => HasValidationItems); }
            set { SetProperty(() => HasValidationItems, value); }
        }

        public ObservableCollection<ValidationResultItem> Items
        {
            get { return GetProperty(() => Items); }
            private set { SetProperty(() => Items, value); }
        }

        public string NewBarcode
        {
            get { return GetProperty(() => NewBarcode); }
            set { SetProperty(() => NewBarcode, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public bool OkIsEnabled
        {
            get { return GetProperty(() => OkIsEnabled); }
            private set { SetProperty(() => OkIsEnabled, value); }
        }

        public string OkContent
        {
            get { return GetProperty(() => OkContent); }
            private set { SetProperty(() => OkContent, value); }
        }

        public bool IsNo { get; private set; }

        protected override Task HandleOkAsync()
        {
            IsOk = true;
            Close();
            return Task.CompletedTask;
        }

        protected override Task HandleLoadedAsync()
        {
            countdownTimer = new DispatcherTimer();

            countdownTimer.Tick += CountdownTimerTick;
            countdownTimer.Interval = TimeSpan.FromSeconds(1);

            secondsLeft = TotalSecondsToWait;
            OkContent = $"{OkString} ({secondsLeft.ToString(CultureInfo.InvariantCulture)})";

            countdownTimer.Start();

            Title = "Вы подтверждаете?";

            return base.HandleLoadedAsync();
        }

        protected override void OnParameterChanged(object parameter)
        {
            if (IsInDesignMode)
            {
                return;
            }

            ProductSelectionValidationViewModelParameter param = (ProductSelectionValidationViewModelParameter)parameter;

            NewBarcode = param.NewBarcode;
            ProductName = param.ProductName;
            Items = ValidateProduct(param.ExistingBarcodes).ToObservableCollection();

            HasErrors = Items.Any(x => x.IsError);
            HasValidationItems = Items.Any();
        }

        private IEnumerable<ValidationResultItem> ValidateProduct(IReadOnlyCollection<string> existingBarcodes)
        {
            if (existingBarcodes.Any())
            {
                string errorMessage = string.IsNullOrEmpty(NewBarcode)
                    ? "У этого товара есть ШК"
                    : "Этому товару уже присвоен другой ШК";

                yield return new ValidationResultItem(errorMessage, false);
            }
        }

        private void HandleNo()
        {
            IsNo = true;
            Close();
        }

        private void HandlePreviewKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Y:
                    if (!countdownTimer.IsEnabled)
                    {
                        OkCommand.Execute(null);
                    }

                    break;
                case Key.N:
                    NoCommand.Execute(null);
                    break;
                case Key.Escape:
                    CancelCommand.Execute(null);
                    break;
            }
        }

        private void HandleUnloaded()
        {
            countdownTimer.Stop();
            countdownTimer.Tick -= CountdownTimerTick;
        }

        private void CountdownTimerTick(object sender, EventArgs e)
        {
            secondsLeft--;

            OkContent = $"{OkString} ({secondsLeft.ToString(CultureInfo.InvariantCulture)})";

            if (secondsLeft == 0)
            {
                OkContent = $"{OkString} (Y)";
                OkIsEnabled = true;

                countdownTimer.Stop();
            }
        }
    }
}
