using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DevExpress.Mvvm;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Authentication;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels
{
    internal sealed class LoginViewModel : ViewModelBase
    {
#if DEBUG || DEBUGIIS
        private bool needAutoLogin = true;
#else
        private bool needAutoLogin = false;
#endif

        public LoginViewModel(IWebClient webClient, IMessenger messenger, IMessageFacadeService messageFacadeService)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            MessageFacadeService = messageFacadeService ?? throw new ArgumentNullException(nameof(messageFacadeService));

            ErrorText = string.Empty;

            AuthenticateCommand = new AsyncCommand(AuthenticateAsync, CanAuthenticate);
            CancelCommand = new DelegateCommand(Cancel);
            HandleKeyUpCommand = new DelegateCommand<KeyEventArgs>(HandleKeyUp);
            HandleLoadedCommand = new DelegateCommand(HandleLoaded);
            HandlePasswordBoxKeyDownCommand = new DelegateCommand<KeyEventArgs>(HandlePasswordBoxKeyDown);
            IsVisibleChangedCommand = new DelegateCommand<DependencyPropertyChangedEventArgs>(IsVisibleChanged);
        }

        public LoginViewModel()
        {
        }

        #region Dependency properties

        public string ErrorText
        {
            get { return GetProperty(() => ErrorText); }
            set { SetProperty(() => ErrorText, value); }
        }

        public string Login
        {
            get { return GetProperty(() => Login); }
            set { SetProperty(() => Login, value); }
        }

        public string Password
        {
            get { return GetProperty(() => Password); }
            set { SetProperty(() => Password, value); }
        }

        #endregion

        #region Commands

        public IAsyncCommand AuthenticateCommand { get; }

        public IDelegateCommand CancelCommand { get; }

        public IDelegateCommand HandleKeyUpCommand { get; }

        public IDelegateCommand HandleLoadedCommand { get; }

        public IDelegateCommand HandlePasswordBoxKeyDownCommand { get; }

        public IDelegateCommand IsVisibleChangedCommand { get; }

        #endregion

        private IWebClient WebClient { get; }

        private IMessenger Messenger { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private async Task AuthenticateAsync()
        {
            AuthResponse authResponse = await WebClient.AuthenticateAsync(new AuthRequest(Login, Password));

            if (authResponse.Success)
            {
                Messenger.Send(new PasswordSendMessage(Password));

                ErrorText = null;
                Login = null;
                Password = null;

                EmployeeContextDto authenticatedEmployee = WebClient.AuthenticatedEmployee;

                Messenger.Send(new UserLoggedInMessage(
                    authenticatedEmployee.Id,
                    authenticatedEmployee.Name,
                    authenticatedEmployee.Roles,
                    authenticatedEmployee.AllowedOperations.Cast<BusinessOperation>().ToArray()));
            }
            else
            {
                ErrorText = authResponse.ErrorMessage;
                MessageFacadeService.ShowNotificationError(ErrorText);
            }
        }

        private bool CanAuthenticate()
        {
            return !string.IsNullOrEmpty(Login) && !string.IsNullOrEmpty(Password);
        }

        private void Cancel()
        {
            Messenger.Send(new UserCancelLoginMessage());
        }

        private void HandleKeyUp(KeyEventArgs eventArgs)
        {
            switch (eventArgs.Key)
            {
                case Key.Escape:
                    CancelCommand.Execute(null);
                    break;
            }
        }

        private void HandlePasswordBoxKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    AuthenticateCommand.Execute(null);
                    break;
            }
        }

        private void HandleLoaded()
        {
        }

        private void IsVisibleChanged(DependencyPropertyChangedEventArgs obj)
        {
#if DEBUG || DEBUGIIS
            if (needAutoLogin && obj.NewValue is bool isVisible && isVisible)
            {
                needAutoLogin = false;

                Login = "demo";
                Password = "W7H5sk4j7NlJR4WiC1M5";
                AuthenticateCommand.Execute(null);
            }
#endif
        }
    }
}