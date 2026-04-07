using System;
using System.Threading.Tasks;
using System.Windows;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Options;
using Telemart.Client.Data.Requests.Features.Cronicle;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects.Cronicle;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Discussions;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Parser.Monitoring
{
    public sealed class ParserCronicleViewModel : TelemartViewModelBase
    {
        private const string Schedule = "#Schedule";
        private const string Login = "#Login";
        private const int DiscussionTypeId = 139;
        private static string _sessionId;
        private static string _login;

        private volatile bool setLocalStorage;
        private volatile bool loginRedirect;

        private WebView2 _webView2;

        public ParserCronicleViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            CronicleOptions options,
            IErrorHandler errorHandler,
            IMessenger messenger,
            ILogger<ParserCronicleViewModel> logger)
            : base(webClient, dictionaries, messageFacadeService, logger)
        {
            ErrorHandler = errorHandler;
            Messenger = messenger;

            ParserCronicleOptions = options;
            RefreshCommand = new DelegateCommand(Refresh);
            WebView2LoadedCommand = new DelegateCommand<RoutedEventArgs>(WebView2Loaded);

            NavigationStartingCommand = new AsyncCommand<CoreWebView2NavigationStartingEventArgs>(NavigationStartingAsync);
            NavigationCompletedCommand = new AsyncCommand<CoreWebView2NavigationCompletedEventArgs>(NavigationCompletedAsync);
            CreateDiscussionCommand = new DelegateCommand(CreateDiscussion);

            NavigationUrl = LoginUrl;

            Messenger.Register<ParserCronicleMessage>(this, OnParserCronicleMessageAsync);
        }

        public string NavigationUrl
        {
            get { return GetProperty(() => NavigationUrl); }
            set { SetProperty(() => NavigationUrl, value); }
        }

        public string StartUrl => $"{ParserCronicleOptions.BaseAddress}/{Schedule}";

        public string LoginUrl => $"{ParserCronicleOptions.BaseAddress}/{Login}";

        public IAsyncCommand NavigationStartingCommand { get; }

        public IAsyncCommand NavigationCompletedCommand { get; }

        public IDelegateCommand WebView2LoadedCommand { get; }

        public IDelegateCommand RefreshCommand { get; }

        public IDelegateCommand CreateDiscussionCommand { get; }

        private CronicleOptions ParserCronicleOptions { get; }

        private IErrorHandler ErrorHandler { get; }

        private IMessenger Messenger { get; }

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>(
            "SizeableDialogDocumentManagerService",
            ServiceSearchMode.PreferParents);

        protected override Task HandleLoadedAsync()
        {
            NavigationUrl = LoginUrl;

            return Task.CompletedTask;
        }

        protected override Task HandleUnloadedAsync()
        {
            _webView2.Dispose();
            return base.HandleUnloadedAsync();
        }

        private void Refresh()
        {
            _webView2?.Reload();
        }

        private async Task NavigationStartingAsync(CoreWebView2NavigationStartingEventArgs eventArgs)
        {
            if (_webView2 != null && !setLocalStorage)
            {
                bool isAlive = !string.IsNullOrEmpty(_sessionId) && !string.IsNullOrEmpty(_login);

                if (isAlive)
                {
                    isAlive = await CheckAliveSessionAsync();
                }

                if (!isAlive)
                {
                    await GetSessionFromCronicleAsync();
                }

                Task setusernameTask = _webView2.CoreWebView2.ExecuteScriptAsync($"javascript:localStorage.setItem(\"username\",\"{_login}\")");
                Task setsessionTask = _webView2.CoreWebView2.ExecuteScriptAsync($"javascript:localStorage.setItem(\"session_id\",\"{_sessionId}\")");

                await Task.WhenAll(setusernameTask, setsessionTask);

                NavigationUrl = StartUrl;
                loginRedirect = true;

                setLocalStorage = true;
            }
        }

        private Task NavigationCompletedAsync(CoreWebView2NavigationCompletedEventArgs args)
        {
            if (setLocalStorage && !loginRedirect)
            {
                NavigationUrl = StartUrl;
                loginRedirect = true;
            }

            return Task.CompletedTask;
        }

        private void WebView2Loaded(RoutedEventArgs eventArgs)
        {
            _webView2 = eventArgs.Source as WebView2;
        }

        private async Task GetSessionFromCronicleAsync()
        {
            Result<CronicleSessionDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new GetCronicleSeesion()),
                "получении сессии",
                null,
                this,
                false,
                false);

            if (result?.IsSuccess == true)
            {
                _sessionId = result.Data?.SessionId;
                _login = result.Data?.Login;
            }
        }

        private async void OnParserCronicleMessageAsync(ParserCronicleMessage parserCronicleMessage)
        {
            try
            {
                if (parserCronicleMessage.LogoutClient && !string.IsNullOrEmpty(_sessionId))
                {
                    Result<object> result = await ErrorHandler.HandleErrorsAsync(
                        _ => WebClient.ExecuteApiRequestAsync(new LogoutCronicle(_sessionId)),
                        "закрытии сессии",
                        null,
                        this,
                        false,
                        false);

                    if (result?.IsSuccess != true)
                    {
                        Logger.LogWarning("Ошибка при завершении сессии в Cronicle");
                    }

                    _sessionId = null;
                    _login = null;
                }
            }
            catch (Exception)
            {
            }
        }

        private async Task<bool> CheckAliveSessionAsync()
        {
            if (!string.IsNullOrEmpty(_sessionId))
            {
                Result<AliveSessionCronicleDto> result = await ErrorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new AliveSessionCronicle(_sessionId)),
                    null,
                    null,
                    this,
                    false,
                    false);

                if (result?.IsSuccess == true && result.Data?.IsAlive == true)
                {
                    return true;
                }

                _sessionId = null;
                _login = null;

                if (result?.IsSuccess != true)
                {
                    Logger.LogWarning("Ошибка при проверке сессии в Cronicle");
                }

                if (result?.Data?.IsAlive == false)
                {
                    Logger.LogWarning("Текущая сессия не жива в Cronicle");
                }
            }

            return false;
        }

        private void CreateDiscussion()
        {
            SizeableDialogDocumentManagerService.ShowView<DiscussionViewModel>(
                new DiscussionParameter(0, 0, 0, false, DiscussionTypeId),
                this);
        }
    }
}