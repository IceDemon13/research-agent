using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Options;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Employee.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.WorkAccount;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels
{
    public sealed class WatsNewViewModel : TelemartViewModelBase
    {
        private string _baseTelewikiUrl;
        private string _whatsNew;
        private WebView2 _webView2;
        private ReadOnlyCollection<Cookie> _cookieAutorisations;
        private EmployeeAccountDto _employeeAccountDto;
        private volatile bool _isAutorisation;
        private string _userPassword;

        public WatsNewViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IWikiHelper wikiHelper,
            TelewikiOptions telewikiOptions,
            IErrorHandler errorHandler,
            IMessenger messenger,
            ILogger<WatsNewViewModel> logger)
            : base(webClient, dictionaries, messageFacadeService, logger)
        {
            WikiHelper = wikiHelper;
            ErrorHandler = errorHandler;
            Messenger = messenger;

            WhatsNewCommand = new DelegateCommand(WhatsNew);
            AboutWikiCommand = new DelegateCommand(AboutWiki);
            NavigationStartingCommand = new AsyncCommand<CoreWebView2NavigationStartingEventArgs>(NavigationStartingAsync);
            WebView2LoadedCommand = new DelegateCommand<RoutedEventArgs>(WebView2Loaded);

            TelewikiOptions = telewikiOptions;
        }

        public string NavigationUrl
        {
            get { return GetProperty(() => NavigationUrl); }
            set { SetProperty(() => NavigationUrl, value); }
        }

        public IDelegateCommand WhatsNewCommand { get; }

        public IDelegateCommand AboutWikiCommand { get; }

        public IAsyncCommand NavigationStartingCommand { get; }

        public IDelegateCommand WebView2LoadedCommand { get; }

        private IWikiHelper WikiHelper { get; }

        private TelewikiOptions TelewikiOptions { get; }

        private IErrorHandler ErrorHandler { get; }

        private IMessenger Messenger { get; }

        protected override async Task HandleLoadedAsync()
        {
            _baseTelewikiUrl = TelewikiOptions?.BaseAddress;

            _whatsNew = Dictionaries.GetItemByName<ModuleHelpUrl>(nameof(WatsNewViewModel))?.Url;

            TelewikiParameter telewikiParameter = null;

            telewikiParameter = (TelewikiParameter)Parameter;

            _userPassword = telewikiParameter.UserPassword;

            if (string.Equals(WebClient.AuthenticatedEmployee.Name, "demo",  StringComparison.OrdinalIgnoreCase))
            {
                NavigationUrl = _baseTelewikiUrl;

                return;
            }

            if (!telewikiParameter.IsReloadClient)
            {
                _employeeAccountDto = await LoadEmployeeAcountInfoAsync();

                if (_employeeAccountDto?.AccountInfoDto != null && !string.IsNullOrEmpty(_employeeAccountDto.AccountInfoDto.Email) && !string.IsNullOrEmpty(_userPassword))
                {
                    CookieWiki cookieWiki = await GetCookieWikiAsync();

                    _cookieAutorisations = cookieWiki?.Cookies;
                }
                else if (telewikiParameter.AllowCreate && string.IsNullOrEmpty(telewikiParameter?.Url))
                {
                    _employeeAccountDto = await CreateTelewikiAccountAsync();
                }
            }
            else if (telewikiParameter.Cookies?.Count > 0)
            {
                _cookieAutorisations = telewikiParameter.Cookies;
            }

            NavigationUrl = !string.IsNullOrEmpty(telewikiParameter.Url)
                ? telewikiParameter.Url
                : string.IsNullOrEmpty(_whatsNew) ? _baseTelewikiUrl : _whatsNew;

            Messenger.Send(new SendWikiCookiesMessage(_cookieAutorisations));
        }

        protected override Task HandleUnloadedAsync()
        {
            _webView2.Dispose();
            return base.HandleUnloadedAsync();
        }

        private void WhatsNew()
        {
           _webView2.Source = new Uri(_whatsNew);
        }

        private void AboutWiki()
        {
            _webView2.Source = new Uri(_baseTelewikiUrl);
        }

        private async Task<EmployeeAccountDto> LoadEmployeeAcountInfoAsync()
        {
            EmployeeRichDto employee = await WebClient.ExecuteApiRequestAsync(new QueryEmployee(WebClient.AuthenticatedEmployee.Id));

            return employee?.Accounts?.FirstOrDefault(x => x.AccountId == WorkAccountIds.TelewikiId);
        }

        private async Task<EmployeeAccountDto> CreateTelewikiAccountAsync()
        {
            Result<EmployeeRichDto> employeeResult = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CreateTelewikiAccount(WebClient.AuthenticatedEmployee.Id, _userPassword)),
                "создании учетки в Телевики",
                null,
                this,
                false,
                showDialog: true,
                showNotification: false);

            return employeeResult?.Data?.Accounts?.FirstOrDefault(x => x.AccountId == WorkAccountIds.TelewikiId);
        }

        private void WebView2Loaded(RoutedEventArgs eventArgs)
        {
            if (eventArgs.Source is WebView2)
            {
                _webView2 = (WebView2)eventArgs.Source;
            }
        }

        private async Task<CookieWiki> GetCookieWikiAsync()
        {
            _employeeAccountDto ??= await LoadEmployeeAcountInfoAsync();

            return await WikiHelper.GetCookieAutorisationAsync(_baseTelewikiUrl, _employeeAccountDto?.AccountInfoDto?.Email, _userPassword);
        }

        private async Task NavigationStartingAsync(CoreWebView2NavigationStartingEventArgs eventArgs)
        {
            if (_webView2 != null)
            {
                if (!_isAutorisation)
                {
                    if (_cookieAutorisations?.Any() != true)
                    {
                        CookieWiki cookieWiki = await GetCookieWikiAsync();

                        _cookieAutorisations = cookieWiki?.Cookies;
                    }

                    if (_cookieAutorisations == null)
                    {
                        return;
                    }

                    SetCookieInWebView2();

                    _isAutorisation = true;
                }
            }
        }

        private void SetCookieInWebView2()
        {
            WebView2 webView2 = _webView2;

            if (_cookieAutorisations?.Any() == true)
            {
                foreach (Cookie cookieAutorisation in _cookieAutorisations)
                {
                    CoreWebView2Cookie cookie = webView2.CoreWebView2.CookieManager.CreateCookieWithSystemNetCookie(cookieAutorisation);
                    cookie.IsHttpOnly = false;
                    cookie.IsSecure = false;
                    webView2.CoreWebView2.CookieManager.AddOrUpdateCookie(cookie);
                }
            }

            webView2.CoreWebView2.Navigate(NavigationUrl);
        }
    }
}