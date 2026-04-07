using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using DevExpress.Mvvm;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Common
{
    public sealed class DelayedConfirmViewModel : TelemartDialogViewModelBase
    {
        private const string OkString = "Да";

        private int _totalSecondsToWait = 5;
        private DispatcherTimer _countdownTimer;
        private int _secondsLeft;

        public DelayedConfirmViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            IsNo = false;

            NoCommand = new DelegateCommand(HandleNo);
            HandlePreviewKeyDownCommand = new DelegateCommand<KeyEventArgs>(HandlePreviewKeyDown);
            HandleUnloadedCommand = new DelegateCommand(HandleUnloaded);
        }

        public DelayedConfirmViewModel()
        {
        }

        #region Commands

        public IDelegateCommand NoCommand { get; }

        public IDelegateCommand HandlePreviewKeyDownCommand { get; }

        public IDelegateCommand HandleUnloadedCommand { get; }

        #endregion

        public string Text
        {
            get { return GetProperty(() => Text); }
            private set { SetProperty(() => Text, value); }
        }

        public string YesText
        {
            get { return GetProperty(() => YesText); }
            private set { SetProperty(() => YesText, value); }
        }

        public bool YesEnabled
        {
            get { return GetProperty(() => YesEnabled); }
            private set { SetProperty(() => YesEnabled, value); }
        }

        public bool IsNo { get; private set; }

        protected override Task HandleLoadedAsync()
        {
            if (Parameter is DelayedConfirmParameter parameter)
            {
                Text = parameter.Text;
                _totalSecondsToWait = parameter.TotalSecondsToWait;
            }
            else
            {
                Text = Parameter as string;
            }

            Title = "Подтвержение";

            _countdownTimer = new DispatcherTimer();

            _countdownTimer.Tick += CountdownTimerTick;
            _countdownTimer.Interval = TimeSpan.FromSeconds(1);

            _secondsLeft = _totalSecondsToWait;

            YesText = GetYesText(_totalSecondsToWait.ToString(CultureInfo.InvariantCulture));
            YesEnabled = false;

            _countdownTimer.Start();

            return base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            IsOk = true;
            Close();
            return Task.CompletedTask;
        }

        private void HandleUnloaded()
        {
            _countdownTimer.Stop();
            _countdownTimer.Tick -= CountdownTimerTick;
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
                    if (!_countdownTimer.IsEnabled)
                    {
                        OkCommand.Execute(null);
                        e.Handled = true;
                    }

                    break;
                case Key.N:
                    NoCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Escape:
                    CancelCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }

        private void CountdownTimerTick(object sender, EventArgs e)
        {
            _secondsLeft--;

            YesText = GetYesText(_secondsLeft.ToString(CultureInfo.InvariantCulture));

            if (_secondsLeft == 0)
            {
                YesText = GetYesText("Y");
                YesEnabled = true;

                _countdownTimer.Stop();
            }
        }

        private string GetYesText(string s)
        {
            return $"{OkString} ({s})";
        }
    }
}
